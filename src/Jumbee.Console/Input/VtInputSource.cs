namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Win32.SafeHandles;

/// <summary>
/// An <see cref="IInputSource"/> that puts the terminal into VT input mode and enables mouse, bracketed-paste, and
/// focus reporting, then reads the raw stdin byte stream, decodes it (UTF-8) and runs it through
/// <see cref="AnsiInputDecoder"/> to produce <see cref="TerminalInputEvent"/>s.
/// </summary>
/// <remarks>
/// <para>
/// Restores all terminal state on <see cref="Dispose"/> — pass one to <see cref="UI.Start"/> and it is disposed by
/// <see cref="UI.Stop"/>.
/// </para>
/// <para>
/// Reading runs on a dedicated background thread holding a single outstanding read; an idle timeout flushes the
/// decoder so a lone ESC keypress resolves to <see cref="ConsoleKey.Escape"/> instead of waiting for the next byte.
/// Requires a real interactive terminal; not used by the headless/test paths (which inject their own source).
/// </para>
/// </remarks>
public sealed class VtInputSource : IInputSource, IDisposable
{
    #region Constructors
    /// <param name="idleFlushMs">Idle timeout before flushing a dangling escape sequence.</param>
    /// <param name="anyMotion">
    /// When <see langword="true"/>, request any-motion mouse tracking (DEC 1003) so the pointer is reported on
    /// every move (enabling hover), instead of only while a button is held (DEC 1002). Costs more input traffic.
    /// </param>
    public VtInputSource(int idleFlushMs = 40, bool anyMotion = false)
    {
        _idleFlushMs = idleFlushMs;
        _mode = TerminalInputMode.Enable(anyMotion);
        // On Unix read the raw tty fd we put into raw mode DIRECTLY (stdin when it's a tty, else the /dev/tty we
        // opened) rather than Console.OpenStandardInput() — .NET's Console stdin machinery doesn't reliably deliver
        // raw, byte-at-a-time interactive input on Unix. ownsHandle:false: TerminalInputMode owns/closes the fd.
        _stdin = (!OperatingSystem.IsWindows() && _mode.InputFd >= 0)
            ? new FileStream(new SafeFileHandle(new IntPtr(_mode.InputFd), ownsHandle: false), FileAccess.Read)
            : Console.OpenStandardInput();
        Log($"start win={OperatingSystem.IsWindows()} fd={_mode.InputFd} owns={_mode.OwnsInputFd} raw={_mode.RawModeApplied} stream={_stdin.GetType().Name}");
        _reader = new Thread(ReaderLoop) { IsBackground = true, Name = "Jumbee.VtInput" };
        // Do not read at all if the terminal refused VT input mode: this source cannot work there (see
        // VtModeUnavailable), and starting the loop would do ACTIVE HARM that outlives it. The loop keeps one
        // outstanding _stdin.ReadAsync, and a console read cannot be cancelled — Dispose ends the thread but the
        // read stays pending on the console handle for the life of the process. The replacement source
        // (ConsoleInputSource, via ReadConsoleInput) then competes with that orphan for one input queue: keys
        // alternate between them, so roughly every other press vanishes, and with the console still in cooked mode
        // conhost's line editor consumes the arrows outright. Never issuing the read leaves nothing to orphan.
        if (!VtModeUnavailable) _reader.Start();
        else Log("VT input mode unavailable — reader not started (source is unusable; expect UI.Start to replace it)");
    }

    // Test seam: runs the reader loop over an arbitrary stream without touching the real terminal's input mode, so
    // the loop's behaviour when a read FAILS can be exercised. That path is otherwise unreachable in a test, and it
    // is the one that used to kill the reader thread outright.
    internal VtInputSource(Stream stdin, int idleFlushMs)
    {
        _idleFlushMs = idleFlushMs;
        _mode = null;
        _stdin = stdin;
        _reader = new Thread(ReaderLoop) { IsBackground = true, Name = "Jumbee.VtInput.Test" };
        _reader.Start();
    }
    #endregion

    #region Methods
    /// <summary>
    /// <see langword="true"/> when this source is attached to a real terminal that <b>refused</b> VT input mode —
    /// the state in which it cannot work and must not be used.
    /// </summary>
    /// <remarks>
    /// The failure is silent and badly asymmetric: the reader still gets bytes, so the source looks alive, but the
    /// console only encodes arrows, function keys, mouse and paste as VT sequences <em>when the mode is set</em>.
    /// Without it those keys produce no bytes whatsoever, while plain characters and Ctrl+letter — real control
    /// characters — arrive as usual. The app therefore comes up looking fine and navigates with nothing.
    /// <para>Requires a <see cref="TerminalInputMode"/> to have been attempted at all, so the test-seam constructor —
    /// which deliberately never touches a terminal — is never reported as unusable.</para>
    /// </remarks>
    internal bool VtModeUnavailable => _mode is not null && !_mode.RawModeApplied;

    /// <inheritdoc/>
    public bool TryRead(out TerminalInputEvent? evt)
    {
        if (_queue.TryDequeue(out var e)) { evt = e; return true; }
        evt = null;
        return false;
    }

    // Single dedicated reader: keep exactly one outstanding ReadAsync; on its idle timeout (no new bytes) flush
    // the decoder once so a dangling ESC resolves. The decoder is touched only on this thread; the queue is the
    // only cross-thread handoff.
    private void ReaderLoop()
    {
        var bytes = new byte[1024];
        var chars = new char[1024];
        var utf8 = Encoding.UTF8.GetDecoder();
        Task<int>? read = null;
        var flushedWhileIdle = false;
        var loggedEof = false;
        Log("reader thread started");

        var failures = 0;

        while (_running)
        {
            read ??= _stdin.ReadAsync(bytes, 0, bytes.Length);

            bool ready;
            int n = 0;
            try
            {
                // BOTH of these can throw, and it must be caught in one place. Task.Wait rethrows a faulted task's
                // exception (wrapped in an AggregateException) just as .Result does, so catching only around .Result
                // — which is what this did — leaves the Wait itself unguarded and any read failure escapes as an
                // unhandled exception that kills the reader thread. On Windows a console read can fail transiently
                // (a resize while a read is outstanding has been seen to surface ERROR_PIPE_NOT_CONNECTED), and
                // losing all keyboard input to a crash dialog is far worse than the blip that caused it.
                ready = read.Wait(_idleFlushMs);
                if (ready) n = read.Result;
            }
            catch (Exception ex)
            {
                // The task is complete (faulted or cancelled), so drop it and start a fresh read.
                read = null;
                if (!_running) { Log($"read failed during shutdown: {ex.GetType().Name}: {ex.Message}"); break; }

                // Retry rather than give up: a transient failure should cost a frame of input, not the session. The
                // backoff after a few consecutive failures keeps a permanently broken stdin from spinning a core —
                // the thread stays alive so input resumes if the console does, and Dispose still ends it.
                failures++;
                if (failures <= 3 || failures % 40 == 0) Log($"read failed ({failures}): {ex.GetType().Name}: {ex.Message}");
                Thread.Sleep(failures > 3 ? BrokenInputBackoffMs : _idleFlushMs);
                continue;
            }

            failures = 0;
            if (ready)
            {
                read = null;
                if (n <= 0) { if (!loggedEof) { Log("read returned EOF (<=0)"); loggedEof = true; } Thread.Sleep(_idleFlushMs); continue; }
                loggedEof = false;
                if (LogPath is not null) Log($"read {n} bytes: {Convert.ToHexString(bytes, 0, Math.Min(n, 16))}");

                int charCount = utf8.GetChars(bytes, 0, n, chars, 0);
                if (charCount > 0)
                {
                    Enqueue(_decoder.Feed(chars.AsSpan(0, charCount)));
                    flushedWhileIdle = false;
                }
            }
            else if (!flushedWhileIdle)
            {
                Enqueue(_decoder.Flush());
                flushedWhileIdle = true;
            }
        }

        Log("reader thread exiting");
    }

    private void Enqueue(IReadOnlyList<TerminalInputEvent> events)
    {
        foreach (var e in events) _queue.Enqueue(e);
    }

    // Opt-in input diagnostics: set JUMBEE_INPUT_LOG=/path to capture what the reader thread sees (handy for the
    // untested-on-this-host Unix input path). No-op when the env var is unset.
    private static readonly string? LogPath = Environment.GetEnvironmentVariable("JUMBEE_INPUT_LOG");
    private static void Log(string msg)
    {
        if (LogPath is null) return;
        try { File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} {msg}{Environment.NewLine}"); } catch { }
    }

    /// <summary>
    /// Announces on stderr that input logging is on, so a forgotten <c>JUMBEE_INPUT_LOG</c> can't quietly cost every
    /// run. A no-op when the variable is unset.
    /// </summary>
    /// <param name="starting">
    /// <see langword="true"/> for the notice before the UI takes the screen, <see langword="false"/> for the one
    /// after it gives it back — which also reports how much the run wrote.
    /// </param>
    /// <remarks>
    /// Both notices are needed and neither is redundant. The startup one is the only warning that arrives before the
    /// cost is paid, but it is easily missed: the UI paints over it a moment later, and on a console with no
    /// alternate screen to restore it is simply cleared. The shutdown one is written after the terminal has been
    /// handed back, so it always survives — and the byte count is what actually conveys the cost, since the log is
    /// appended to on every read (each one an open/write/close).
    /// </remarks>
    internal static void AnnounceLogging(bool starting)
    {
        if (LogPath is null) return;

        try
        {
            if (starting)
            {
                Console.Error.WriteLine($"jumbee: input logging is ON — appending every input read to {LogPath}. " +
                                        "Unset JUMBEE_INPUT_LOG to turn it off.");
            }
            else
            {
                var size = new FileInfo(LogPath) is { Exists: true } f ? $"{f.Length:N0} bytes" : "no data";
                Console.Error.WriteLine($"jumbee: input logging was ON (JUMBEE_INPUT_LOG) — wrote {size} to {LogPath}.");
            }

            Console.Error.Flush();
        }
        catch { /* diagnostics must never break startup or shutdown */ }
    }

    /// <summary>Stops the reader thread and restores the console mode (disabling mouse/paste/focus reporting).</summary>
    public void Dispose()
    {
        if (!_running) return;
        _running = false;     // reader exits on its next idle timeout (it is a background thread)
        _mode?.Dispose();     // disable reporting + restore the original console mode (null on the test seam)
    }
    #endregion

    #region Fields
    // How long to wait between read attempts once stdin has failed repeatedly. Long enough that a permanently
    // broken input costs nothing, short enough that recovery is imperceptible.
    private const int BrokenInputBackoffMs = 250;

    private readonly AnsiInputDecoder _decoder = new();
    private readonly System.Collections.Concurrent.ConcurrentQueue<TerminalInputEvent> _queue = new();
    private readonly Stream _stdin;
    private readonly Thread _reader;
    private readonly TerminalInputMode? _mode;   // null only on the internal test-seam constructor
    private readonly int _idleFlushMs;
    private volatile bool _running = true;
    #endregion
}

/// <summary>
/// Enables VT input mode + mouse/bracketed-paste/focus reporting and restores the prior state on dispose.
/// </summary>
/// <remarks>
/// <para>
/// On Windows this adjusts the console input mode via the Win32 API; on Unix it puts the tty into raw mode via
/// libc <c>cfmakeraw</c> (so reads are byte-at-a-time, unbuffered, and not echoed). The DEC private-mode toggles
/// are emitted as ANSI on all platforms.
/// </para>
/// <para>
/// Ctrl+C is delivered as the byte <c>0x03</c> (a key event) rather than raising SIGINT — on Unix because raw mode
/// disables <c>ISIG</c>, on Windows because <c>ENABLE_PROCESSED_INPUT</c> is cleared. So it reaches the app/shell
/// uniformly (a terminal forwards it to interrupt the foreground program); the app owns its own quit affordance
/// (e.g. Ctrl+Q). Ctrl+Break still raises a console event on Windows, so a hard escape remains.
/// </para>
/// </remarks>
internal sealed class TerminalInputMode : IDisposable
{
    // SGR mouse (1006) + mouse motion tracking + bracketed paste (2004) + focus (1004). Motion is either
    // 1002 (report only while a button is held) or, when anyMotion is requested, 1003 (report every move, so
    // hover works at the cost of more input traffic).
    public static TerminalInputMode Enable(bool anyMotion = false) => new(anyMotion);

    private TerminalInputMode(bool anyMotion)
    {
        var motion = anyMotion ? "1003" : "1002";
        var enableSeq = $"\x1b[?{motion}h\x1b[?1006h\x1b[?2004h\x1b[?1004h";
        _disableSeq = $"\x1b[?1004l\x1b[?2004l\x1b[?1006l\x1b[?{motion}l";

        if (OperatingSystem.IsWindows())
        {
            _stdinHandle = GetStdHandle(STD_INPUT_HANDLE);
            if (GetConsoleMode(_stdinHandle, out _originalMode))
            {
                var mode = _originalMode;
                // Clear PROCESSED_INPUT too so Ctrl+C is delivered as the byte 0x03 (a key event) instead of being
                // eaten as a console Ctrl+C signal — matching Unix raw mode, so a terminal can forward it to the
                // shell. (Ctrl+Break is unaffected and still raises an event, leaving a hard Windows escape.)
                mode &= ~(ENABLE_PROCESSED_INPUT | ENABLE_LINE_INPUT | ENABLE_ECHO_INPUT | ENABLE_QUICK_EDIT_MODE);
                mode |= ENABLE_VIRTUAL_TERMINAL_INPUT | ENABLE_EXTENDED_FLAGS;
                _modeChanged = SetConsoleMode(_stdinHandle, mode);
            }
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
        {
            // Resolve the controlling terminal: use stdin when it's a tty, else open /dev/tty so the app still
            // gets raw keyboard input when stdin is redirected (`myapp < file`, `echo x | myapp`) — the same trick
            // fzf/gum use to keep working in a pipeline. _inputFd < 0 (no controlling terminal, e.g. CI) → skip.
            _inputFd = isatty(STDIN_FILENO) == 1 ? STDIN_FILENO : open(DevTty, O_RDWR);
            if (_inputFd >= 0)
            {
                _ownsInputFd = _inputFd != STDIN_FILENO; // opened /dev/tty ourselves → must close it on dispose
                // Save current settings, then switch to raw mode. cfmakeraw also sets VMIN=1/VTIME=0, which is
                // exactly what the byte-at-a-time ReaderLoop wants.
                _savedTermios = new byte[TermiosSize];
                if (tcgetattr(_inputFd, _savedTermios) == 0)
                {
                    var raw = (byte[])_savedTermios.Clone();
                    cfmakeraw(raw);
                    _termiosChanged = tcsetattr(_inputFd, TCSANOW, raw) == 0;
                }
            }
        }

        // Only ask for mouse/paste/focus reporting if the terminal can actually interpret the request. On Windows a
        // console that refused VT input mode has VT processing off, so these sequences are PRINTED rather than
        // obeyed — a burst of `[?1003h[?1006h…` across the user's screen, and nothing enabled in return.
        _reportingEnabled = !OperatingSystem.IsWindows() || _modeChanged;
        if (_reportingEnabled)
        {
            Console.Out.Write(enableSeq);
            Console.Out.Flush();
        }

        // Safety net: restore the terminal even on an abrupt exit (e.g. Ctrl+C ends the process before Stop runs),
        // otherwise it is left in mouse-reporting mode.
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    private void OnProcessExit(object? sender, EventArgs e) => Dispose();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;

        // Symmetric with the enable above: nothing was turned on, so turning it off would only print more garbage.
        if (_reportingEnabled)
        {
            try { Console.Out.Write(_disableSeq); Console.Out.Flush(); }
            catch { /* best effort */ }
        }

        if (_modeChanged) SetConsoleMode(_stdinHandle, _originalMode);
        if (_termiosChanged) tcsetattr(_inputFd, TCSANOW, _savedTermios!);
        if (_ownsInputFd) close(_inputFd);
    }

    /// <summary>The terminal input fd raw mode was applied to — stdin, or a /dev/tty we opened; -1 if none.</summary>
    public int InputFd => _inputFd;

    /// <summary><see langword="true"/> when <see cref="InputFd"/> is a /dev/tty we opened (read from it, not stdin).</summary>
    public bool OwnsInputFd => _ownsInputFd;

    /// <summary><see langword="true"/> when raw/VT input mode was successfully applied (Unix termios or Win console mode).</summary>
    public bool RawModeApplied => _termiosChanged || _modeChanged;

    #region Win32
    private const int STD_INPUT_HANDLE = -10;
    private const uint ENABLE_PROCESSED_INPUT = 0x0001;      // off → Ctrl+C arrives as a 0x03 byte, not a signal
    private const uint ENABLE_LINE_INPUT = 0x0002;
    private const uint ENABLE_ECHO_INPUT = 0x0004;
    private const uint ENABLE_EXTENDED_FLAGS = 0x0080;
    private const uint ENABLE_QUICK_EDIT_MODE = 0x0040;       // intercepts mouse; must be off for mouse reporting
    private const uint ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200; // deliver input as VT sequences

    private readonly IntPtr _stdinHandle;
    private readonly uint _originalMode;
    private readonly bool _modeChanged;
    private readonly bool _reportingEnabled;   // the enable sequence was actually emitted, so Dispose must undo it
    private readonly string _disableSeq;
    private bool _disposed;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
    #endregion

    #region Posix
    private const int STDIN_FILENO = 0;
    private const int TCSANOW = 0;          // apply termios change immediately (same value on Linux/macOS/BSD)
    private const int O_RDWR = 2;           // open() flags — value is identical on Linux/macOS/BSD
    private const string DevTty = "/dev/tty";
    // struct termios is at most ~72 bytes (macOS); over-allocate so tcgetattr's write always fits. The blob is
    // opaque — cfmakeraw does all the field/flag manipulation, so we never depend on the per-platform layout.
    private const int TermiosSize = 128;

    private readonly byte[]? _savedTermios;
    private readonly bool _termiosChanged;
    private readonly int _inputFd = -1;     // stdin or an opened /dev/tty; -1 = not resolved (Windows / no tty)
    private readonly bool _ownsInputFd;     // we opened /dev/tty and must close it

    [DllImport("libc", SetLastError = true)]
    private static extern int isatty(int fd);

    [DllImport("libc", SetLastError = true)]
    private static extern int open([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags);

    [DllImport("libc", SetLastError = true)]
    private static extern int close(int fd);

    [DllImport("libc", SetLastError = true)]
    private static extern int tcgetattr(int fd, [Out] byte[] termios);

    [DllImport("libc", SetLastError = true)]
    private static extern int tcsetattr(int fd, int optionalActions, [In] byte[] termios);

    [DllImport("libc")]
    private static extern void cfmakeraw([In, Out] byte[] termios);
    #endregion
}
