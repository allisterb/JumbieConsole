namespace Jumbee.Console;

using ConsoleGUI;
using ConsoleGUI.Api;
using ConsoleGUI.Input;
using ConsoleGUI.Space;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Manages th overall UI and provides a paint event for controls to subscribe to.
/// </summary>
public static class UI
{
    #region Methods
    /// <summary>
    /// Initializes the console and starts the UI, returning a task that completes when the UI stops.
    /// </summary>
    /// <param name="layout">The root layout to display.</param>
    /// <param name="width">Initial console width in cells. On an ANSI terminal the real size is read and the physical window is never resized.</param>
    /// <param name="height">Initial console height in cells.</param>
    /// <param name="fps">Target repaint rate in frames per second (default 10 ≈ a 100&#160;ms frame). It sets the UI
    /// thread's per-frame interval to <c>1000 / fps</c>&#160;ms — the thread wakes at most once per interval to drain
    /// input and repaint dirty regions. Raise it for smoother animation or live data (e.g. 30–60) at the cost of more
    /// CPU; lower it to idle cheaper. A frame is skipped when nothing is dirty, so a higher <paramref name="fps"/>
    /// only costs CPU while the UI is actually changing. Values &#8804;&#160;0 are treated as the slowest rate and the
    /// interval is floored at 1&#160;ms (~1000&#160;fps max), so no setting can divide-by-zero or busy-loop the thread.</param>
    /// <param name="isAnsiTerminal">Emit ANSI escape sequences (<see langword="true"/>) vs. 16-colour System.Console output (<see langword="false"/>).</param>
    /// <param name="console">An explicit console sink; when <see langword="null"/> one is chosen from <paramref name="isAnsiTerminal"/>.</param>
    /// <param name="input">An explicit input source; when <see langword="null"/> one is chosen for the terminal (see <see cref="DefaultInputSource"/>).</param>
    /// <param name="useAlternateScreen">Run on the alternate screen buffer so the user's prior terminal contents are restored on exit.</param>
    public static Task Start(ILayout layout, int width = 110, int height = 25, int fps = 10, bool isAnsiTerminal = true, IConsole? console = null, IInputSource? input = null, bool useAlternateScreen = true)
    {
        if (isRunning) return runCompletion.Task;
        // Before anything is drawn: say so if input logging is on. It costs a file append per read, and an env var
        // set during a debugging session is easy to leave behind. Paired with the shutdown notice in Stop, which is
        // the one that reliably survives (this one is painted over, and cleared outright where there is no alternate
        // screen to restore).
        VtInputSource.AnnounceLogging(starting: true);
        ProcessMetrics.Start();
        // Complete each frame's record when its terminal write lands — off the UI thread, and after the loop has
        // moved on, which is why the ordinal travels with the write rather than being inferred from position.
        ConsoleManager.FrameWritten = (ordinal, waitMs, writeMs) => ProcessMetrics.RecordWrite(ordinal, waitMs, writeMs);

        // ASK THE CONSOLE BEFORE ASSUMING IT SPEAKS ANSI — and this must come first, ahead of every other write
        // below, because all of them are escape sequences.
        //
        // A Windows console does not interpret escapes until an application enables VT processing, and a console
        // with the machine-wide "Use legacy console" option ticked refuses to enable it at all. `isAnsiTerminal`
        // defaults to true, so before this the library simply assumed — and on a legacy console every sequence was
        // printed as literal text from the first frame: no UI, just `[38;2;148;148;148m` and `[59;1H` scrolling
        // past. Enabling and detecting are the same call, so this both turns VT on where it can be and reports
        // where it cannot.
        //
        // Only a downgrade, never an upgrade: a caller who passed `isAnsiTerminal: false` keeps the legacy path,
        // and a caller-supplied console is a stub or a headless render whose real console must not be touched.
        if (isAnsiTerminal && console is null && !TerminalCapabilities.TryEnableAnsiOutput())
        {
            isAnsiTerminal = false;
        }

        // Self-heal a terminal left in a bad state by a PREVIOUS run that was hard-killed (SIGKILL, Task-Manager "End
        // task", a debugger Stop): no in-process code runs on a hard kill, so that run never disabled mouse/paste/focus
        // reporting and the shell has been echoing stray reports ever since. We can't fix the dying process, but the
        // next run can reset a clean baseline here before configuring its own session — disable every mouse-tracking
        // encoding + bracketed paste + focus reporting, reset SGR, show the cursor. All are idempotent no-ops on a
        // normal start, so this is safe every time. The alternate screen is intentionally NOT toggled here (that would
        // jump the cursor on a normal start); it self-heals via AlternateScreen's own enter/clear/leave plus the clean
        // Stop. Gated to a real interactive ANSI terminal with no caller-supplied console — AND only when we own the
        // input source (input is null). A caller who passes their own input has already configured the terminal: a
        // `new VtInputSource(...)` constructed as the Start argument runs its mouse/paste/focus ENABLE sequence before
        // Start is even called, so self-healing here would disable the mouse it just turned on (keyboard survives —
        // it isn't mode-gated). When input is null the default VtInputSource is created below, AFTER this reset, so its
        // enable correctly lands last. Keyboard-only apps (also input == null) still get the heal.
        if (isAnsiTerminal && console is null && input is null && IsInteractiveTerminal() && !NonInteractiveEnvironment())
        {
            try { Console.Out.Write(TerminalSelfHealSeq); Console.Out.Flush(); } catch { /* best effort */ }
        }
        // Enter the alternate screen FIRST — before ConsoleManager.Console is assigned (setting it runs Initialize,
        // which clears the screen) and before any rendering — so the clear and the whole session land on the alternate
        // buffer and the user's primary screen (their prompt/output) is saved intact and restored on exit. Gated to a
        // real interactive ANSI terminal with no caller-supplied console (a test/headless stub must never emit this to
        // real stdout) — the same gate the default VT input source uses.
        altScreen = (isAnsiTerminal && useAlternateScreen && console is null
            && IsInteractiveTerminal() && !NonInteractiveEnvironment())
            ? AlternateScreen.Enter() : null;

        // TURN AUTOWRAP OFF (DECAWM) for the session, and it is the bottom-right cell that makes this necessary.
        // A full-screen renderer writes every cell, including the last one on the last row. With autowrap on, that
        // write leaves the terminal in "pending wrap" -- and a terminal that resolves that eagerly rather than
        // deferring it SCROLLS THE SCREEN BY ONE ROW. Once that happens the damage is not one bad frame: the
        // renderer diffs against its own model of the screen, so after an unnoticed scroll every cell it thinks is
        // unchanged is now displayed one row off and never gets rewritten. The symptom is stale content smeared
        // across the whole screen and a status line repeating down the bottom, one copy per frame.
        //
        // Deferred wrap is what the spec calls for and what conhost, Windows Terminal and xterm implement, which is
        // why this went unnoticed -- but relying on it is relying on the one corner of VT that terminals most often
        // get wrong. Nothing here needs wrapping: the emitter positions every discontiguous cell with an explicit
        // CUP and never lets a run cross a row boundary, so with DECAWM off the cursor simply parks in the last
        // column and the next row starts with a CUP as usual.
        //
        // Gated exactly like the alternate screen -- a caller-supplied console is a test stub or a headless render
        // and must never have escape codes written to real stdout on its behalf.
        wrapDisabled = isAnsiTerminal && console is null
            && IsInteractiveTerminal() && !NonInteractiveEnvironment();
        if (wrapDisabled)
        {
            try { Console.Out.Write(WrapOffSeq); Console.Out.Flush(); } catch { /* best effort */ }
        }
        // Drives the renderer: ANSI escape sequences when true, IConsole.Write (16-colour System.Console) when false.
        ConsoleManager.AnsiEnabled = isAnsiTerminal;
        if (console != null)
        {
            ConsoleManager.Console = console;
        }
        else if (!isAnsiTerminal)
        {
            // Legacy terminal: 16-colour System.Console output. Input stays keyboard-only (ConsoleInputSource
            // below) since VT mouse/paste/focus aren't available.
            ConsoleManager.Console = new SimplifiedConsole();
        }
        else
        {
            // ANSI path: read the terminal size but never resize the physical terminal. The default StandardConsole
            // would manipulate the window/buffer on every size set, which never converges with the live window size —
            // making ConsoleManager.AdjustBufferSize resize and re-lay-out the whole UI every frame.
            ConsoleManager.Console = new AnsiTerminalConsole();
        }
        inputSource = input ?? DefaultInputSource(isAnsiTerminal);
        // A VT input source that the terminal REFUSED to put into VT mode cannot work, and fails in the worst
        // possible way: it still reads bytes, so it looks alive, but the console only encodes arrows, function keys,
        // mouse and paste as VT sequences when that mode is set. Everything navigational silently produces nothing
        // while Ctrl+letter keeps working, so the app looks fine and moves for no key.
        //
        // The default source already avoids this (DefaultInputSource only builds a VtInputSource on a terminal we
        // believe speaks ANSI), but a CALLER-SUPPLIED one bypasses that decision entirely — and a caller constructing
        // `new VtInputSource(...)` in Start's own argument list has already configured the terminal before this
        // method ran, so it cannot have consulted the probe above. That is the real case: a legacy console refuses VT
        // on both handles, rendering correctly falls back to the Win32 path, and input is left holding a source that
        // eats every arrow. Swap it for the keyboard-only source, which is what works there (no mouse — the console
        // cannot report it).
        if (inputSource is VtInputSource { VtModeUnavailable: true } unusable)
        {
            unusable.Dispose();   // restores the console mode and undoes the mouse/paste/focus enable it emitted
            inputSource = new ConsoleInputSource();
        }

        ConsoleManager.Setup();
        ConsoleManager.Resize(new Size(width, height));
        // Wrap the app's root in a UI-owned system overlay so global modals (the F1 help dialog, and any future
        // system dialog) always have a layer to show on, whatever the app's root layout is. If the root is already
        // an Overlay, reuse it. The overlay is transparent to navigation/input while no popup is shown.
        systemOverlay = layout as Overlay ?? new Overlay(layout);
        ConsoleManager.Content = systemOverlay.CControl;
        UI.layout = systemOverlay;
        foreach(var c in layout.Controls.Select(lc => lc.FocusableControl))
        {
            if (!controls.Contains(c))
            {
                controls.Add(c);
            }               
        }
        // Convert the target fps to the per-frame wait (ms). Clamp the divisor so fps <= 0 can't divide-by-zero, and
        // floor at 1 ms so a very large fps can't collapse to a 0 ms busy-loop (the dispatcher's WaitOne(0) never blocks).
        interval = Math.Max(1, 1000 / Math.Max(1, fps));
        cts = new CancellationTokenSource();
        cancellationToken = cts.Token;
        runCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        isRunning = true;
        // Start the single UI thread (reads the dispatcher queue, then renders each frame).
        dispatcher.Start(OnFrame, interval);
        // Start the input thread which blocks on the console and marshals each key onto the UI thread.
        perfHud.RegisterToggle(HotKeys.CtrlF12);
        inputThread = new Thread(InputLoop) { IsBackground = true, Name = "Jumbee.Console.Input" };
        inputThread.Start();
        RegisterSignalHandlers();        
        return runCompletion.Task;
    }

    /// <summary>
    /// Chooses the input source when the caller passed none: the raw <see cref="VtInputSource"/> (so mouse, paste,
    /// and focus reporting work) on an <em>interactive</em> ANSI terminal, otherwise the keyboard-only
    /// <see cref="ConsoleInputSource"/>. "Interactive" means neither stdin nor stdout is redirected — so a piped or
    /// redirected run (a test host, CI, `app &lt; file`, `app | tee`) keeps the safe keyboard-only source and never
    /// flips a non-terminal into raw mode. Pass an explicit <c>input:</c> to <see cref="Start"/> to override.
    /// </summary>
    internal static IInputSource DefaultInputSource(bool isAnsiTerminal)
    {
        if (isAnsiTerminal && IsInteractiveTerminal() && !NonInteractiveEnvironment())
        {
            // Guard against raw-mode setup failing on an odd host (e.g. no controlling terminal): fall back rather
            // than leaving the app with no working input.
            try { return new VtInputSource(); }
            catch { /* fall through to the keyboard-only source */ }
        }
        return new ConsoleInputSource();
    }

    // A cheap gate on top of the tty check for the common "looks like a terminal but no human is driving it" cases:
    // a CI runner (which allocates a pty yet sets CI), or an explicitly dumb terminal. It is a conservative NEGATIVE
    // signal only — false keeps the door open, true forces keyboard-only. The fully reliable positive test is an ANSI
    // query handshake (write DSR `ESC[6n` / DA `ESC[c`, wait briefly for the terminal's reply); not done here because
    // it costs startup latency and a raw-mode probe. Pass an explicit input source to Start to bypass all of this.
    private static bool NonInteractiveEnvironment()
    {
        try
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"))) return true;   // GitHub/GitLab/Circle/…
            return Environment.GetEnvironmentVariable("TERM") is "dumb";
        }
        catch { return false; }
    }

    /// <summary><see langword="true"/> when both console input and output are real terminals (not redirected/piped),
    /// so it is safe to put the terminal into raw VT input mode.</summary>
    /// <remarks>This is what <see cref="Start"/> uses to decide whether to default to a mouse-capable
    /// <see cref="VtInputSource"/> when no <c>input</c> is supplied.</remarks>
    public static bool IsInteractiveTerminal()
    {
        try { return !Console.IsInputRedirected && !Console.IsOutputRedirected; }
        catch { return false; }
    }

    // Safety hatch: catch external stop signals so a `kill`/`pkill` from another terminal (or the terminal window
    // closing) ALWAYS restores the terminal and exits — even when the UI/input pipeline is wedged and no in-app
    // quit (Ctrl+Q) can fire. Raw mode means Ctrl+C arrives as a byte (it reaches the app/shell, not us), so these
    // handlers respond only to actual signals, not interactive keys.
    private static void RegisterSignalHandlers()
    {
        signalRegistrations = [];
        foreach (var signal in new[] { PosixSignal.SIGTERM, PosixSignal.SIGINT, PosixSignal.SIGQUIT, PosixSignal.SIGHUP })
        {
            try { signalRegistrations.Add(PosixSignalRegistration.Create(signal, OnSignal)); }
            catch (Exception) { /* signal not supported on this platform (e.g. SIGHUP/SIGQUIT on Windows) */ }
        }
    }

    private static void OnSignal(PosixSignalContext context)
    {
        // Restore the terminal synchronously (best effort) so it happens before the process terminates, then let
        // the signal's default action stop us — we do NOT Cancel, so exit is guaranteed even if the loop is stuck.
        isRunning = false;
        // Drain in-flight frame writes first (short bound — a signal must stay responsive) so the restore sequences
        // aren't corrupted by a frame being written to stdout concurrently. See the note in Stop(). The render loop
        // is not stopped here (we let the signal's default action end us), so this is best-effort on an abrupt exit.
        try { ConsoleManager.OutputIdle.Wait(150); } catch { /* best effort */ }
        try { (inputSource as IDisposable)?.Dispose(); } catch { /* best effort */ }
        try { altScreen?.Dispose(); altScreen = null; } catch { /* best effort */ }
        RestoreWrap();
        TerminalCapabilities.Restore();
    }

    // Autowrap back on, paired with the DECAWM reset in Start. Idempotent and self-clearing, so the signal path and
    // a normal Stop can both call it. Emitted after leaving the alternate screen: DECAWM is per-buffer on some
    // terminals, so the mode has to be restored while the PRIMARY screen is the current one or the user is left
    // with a shell that truncates long lines instead of wrapping them.
    private static void RestoreWrap()
    {
        if (!wrapDisabled) return;
        wrapDisabled = false;
        try { Console.Out.Write(WrapOnSeq); Console.Out.Flush(); } catch { /* best effort */ }
    }

    /// <summary>
    /// Reads console keys on a dedicated thread and posts their dispatch onto the UI thread so input is
    /// handled on the same thread as rendering.
    /// </summary>
    private static void InputLoop()
    {
        while (isRunning && !cancellationToken.IsCancellationRequested)
        {
            if (inputSource.TryRead(out var evt))
            {
                var e = evt;
                dispatcher.Post(() => OnInput(e));
            }
            else
            {
                Thread.Sleep(interval / 4);
            }
        }
    }

    /// <summary>Dispatches a terminal input event on the UI thread, routing by kind.</summary>
    private static void OnInput(TerminalInputEvent? evt)
    {
        // An "action" input (a key, click, wheel, or paste — anything but bare pointer motion) forces a full redraw
        // of the frame it is handled on. OnInput drains off the dispatcher queue before that frame's paint/composite,
        // so the whole screen is re-composited with the input's effect. This is a robustness guarantee: many controls
        // (the Tree's selection, an editor's scroll/caret) request a repaint but don't fully localize their damage —
        // they relied on the old redraw-everything-every-frame renderer — so a partial redraw can miss part of their
        // change. Full-redrawing on discrete input is essentially free at human rates and keeps interaction correct.
        // Autonomous updates (animation, throttled self-refresh) and plain mouse motion are unaffected, so idle
        // rendering keeps the dirty-rectangle perf win.
        bool actionInput = false;
        switch (evt)
        {
            case KeyInputEvent k:
                actionInput = true;
                var inputEvent = new InputEvent(k.ToConsoleKeyInfo());
                globalInputListener.OnInput(inputEvent);
                if (!inputEvent.Handled)
                {
                    inputEventArgs.InputEvent = inputEvent;
                    layout?.OnInput(inputEventArgs);
                }

                break;
            case MouseInputEvent m:
                ConsoleManager.MousePosition = new Position(m.X, m.Y);
                // Latch the pressed button so the click handlers that fire on the matching Up (OnClick/OnDoubleClick,
                // dispatched synchronously when MouseDown flips to false) can tell a right-click from a left-click.
                if (m.Kind == TerminalMouseKind.Down) MouseButton = m.Button;
                switch (m.Kind)
                {
                    case TerminalMouseKind.Down: actionInput = true; ConsoleManager.MouseDown = true; break;
                    case TerminalMouseKind.Up: actionInput = true; ConsoleManager.MouseDown = false; break;
                    case TerminalMouseKind.Wheel:
                        actionInput = true;
                        // Position was set above; dispatch the notch to the control under the pointer.
                        ConsoleManager.MouseWheel(m.Button == TerminalMouseButton.WheelUp ? -WheelLines : WheelLines);
                        break;
                        // Move/Drag only need the updated position above.
                }
                break;
            case PasteInputEvent p:
                actionInput = true;
                layout?.OnPaste(p.Text);
                break;
            case FocusInputEvent f:
                HasFocus = f.HasFocus;
                break;
            // ResizeInputEvent is handled by the render loop's terminal-size check, not here.
        }

        if (actionInput)
        {
            ConsoleManager.MarkFullDirty();
            needsDraw = true;
        }
    }
    /// <summary>
    /// Stops the UI update loop and disposes of the timer. 
    /// </summary>
    public static void Stop()
    {
        if (!isRunning) return;
        isRunning = false;
        cts.Cancel();

        // Shut down in an order that restores the terminal cleanly. Frames are written to stdout on a thread-pool
        // thread via ConsoleManager's output sink, whereas the terminal-restore sequences (mouse/focus/paste reporting
        // off, and leaving the alternate screen) are written via Console.Out on this thread. So:
        //   1. Stop the render loop so no more frames are emitted (joins the UI thread when Stop is called off it).
        //   2. Drain any already-queued frame writes.
        //   3. Only then restore the terminal — making the restore sequences the sole, final writers to stdout.
        // Otherwise a restore sequence could interleave with a frame write (splitting the escape, so the terminal
        // keeps reporting mouse/focus after exit — the shell echoes the stray reports as garbage), or a late frame
        // could repaint the primary screen after we've switched back to it. Waits are bounded so a wedged write or a
        // stuck UI thread can never hang exit.
        dispatcher.Stop();
        try { ConsoleManager.OutputIdle.Wait(500); } catch { /* best effort */ }
        (inputSource as IDisposable)?.Dispose();   // mouse/focus/paste off + console-mode restore; unblocks the reader
        altScreen?.Dispose();                      // reset SGR, show cursor, leave the alternate screen
        altScreen = null;
        RestoreWrap();                             // autowrap back on, AFTER the primary screen is current
        TerminalCapabilities.Restore();            // VT processing off LAST — after it, escapes would print as text

        // Wait for the input reader thread to actually exit before returning. It reads the static `inputSource`, so a
        // reader left running from a previous Start would otherwise keep consuming — and reorder — a later run's input
        // (two consumers on one queue). It is a background thread and never the caller, so this join is safe/bounded.
        var reader = inputThread;
        inputThread = null;
        if (reader is not null && reader != Thread.CurrentThread) reader.Join(1000);
        if (signalRegistrations is not null)   // not disposed from a signal callback, so this never self-deadlocks
        {
            foreach (var r in signalRegistrations) r.Dispose();
            signalRegistrations = null;
        }
        controls.Clear();
        RestoreBuiltInHotKeys();
        MouseButton = TerminalMouseButton.None;   // clear transient input state so it can't leak into a later session
        ProcessMetrics.Stop();
        // The terminal is ours to write to again (restored above), so this notice lands on the screen the user is
        // left looking at — and reports what the run actually cost.
        VtInputSource.AnnounceLogging(starting: false);
        runCompletion.TrySetResult();
    }

    /// <summary>Moves keyboard focus to <paramref name="target"/>, clearing focus on all other registered
    /// controls (single-focus).</summary>
    /// <remarks>Used by click-to-focus; runs on the UI thread.</remarks>
    public static void SetFocus(IFocusable target)
    {
        Invoke(() =>
        {
            // Resolve a composite's inner child up to the composite (the navigable focus unit); the composite then
            // delegates focus back to the appropriate child via Control_OnFocus. Click-to-focus and keyboard
            // navigation therefore agree on which control is "focused". Tell the owning composite which child was
            // asked for first, so it delegates back to that one — without this a composite always re-focuses its
            // first child, and clicking any other field in a form would do nothing.
            if (target is Control tc)
            {
                tc.Owner?.OnChildFocusRequest(tc);
                target = tc.FocusRoot;
            }
            var keep = target.FocusableControl;
            foreach (var c in controls)
            {
                // Don't clear controls in the target's own subtree (a composite keeps its delegated child focused
                // when it is (re)focused); clear every other focused control for single-focus.
                if (c.IsFocused && !ReferenceEquals(c, target) && !ReferenceEquals(c, keep)
                    && !(c is Control cc && ReferenceEquals(cc.FocusRoot, target)))
                    c.IsFocused = false;
            }
            if (target.Focusable) target.IsFocused = true;
        });
    }

    /// <summary>The currently focused registered control, or <see langword="null"/> if none.</summary>
    public static IFocusable? Focused => controls.FirstOrDefault(c => c.IsFocused);

    /// <summary>
    /// Opens the global help dialog — a modal with one tab per control (compiled from every control's
    /// <see cref="Control.GetHelpInfo"/>, deduplicated by <see cref="HelpInfo.Name"/>), the focused control's tab
    /// shown first.
    /// </summary>
    /// <remarks>
    /// Bound to <see cref="HotKeys.F1"/> by default; pressing it again closes the dialog. A no-op when
    /// no control supplies help. The dialog is shown on the UI-owned system overlay (see <see cref="Start"/>).
    /// </remarks>
    public static void ShowHelp() => Invoke(() =>
    {
        if (systemOverlay is null) return;
        if (systemOverlay.Top is HelpControl) { systemOverlay.Hide(); return; }   // F1 toggles the dialog

        var infos = CompileHelp();
        if (infos.Count <= 1) return;   // only the built-in General entry — no control supplied help

        // Open on the focused control's tab (resolve an inner composite child up to its owning unit, then match by
        // name since that tab may be a shared/deduplicated entry).
        var focusName = Focused is Control fc ? (fc.FocusRoot.CompileHelp() ?? fc.CompileHelp())?.Name : null;
        var start = 0;
        if (focusName is not null)
            for (var i = 0; i < infos.Count; i++)
                if (infos[i].Name == focusName) { start = i; break; }

        systemOverlay.ShowModal(new HelpControl(infos, HideHelp, start));
    });

    /// <summary>
    /// Compiles the help shown by <see cref="ShowHelp"/>: a built-in "General" entry (global keys) followed by each
    /// registered control's <see cref="Control.GetHelpInfo"/> (modified by its <see cref="Control.OnHelp"/>
    /// handlers), deduplicated by <see cref="HelpInfo.Name"/>.
    /// </summary>
    /// <remarks>Exposed so an app can present its own help UI.</remarks>
    public static IReadOnlyList<HelpInfo> CompileHelp()
    {
        var infos = new List<HelpInfo>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        // A built-in entry for the global keys, always first.
        var general = new HelpInfo("General", text: "Global keys, available anywhere in the app.")
            .WithKey("F1", "Show / hide this help")
            .WithKey("Ctrl+Q", "Quit")
            .WithKey("Ctrl+← ↑ → ↓", "Move focus between regions")
            .WithKey("Ctrl+N / Ctrl+P", "Next / previous control in a region");
        infos.Add(general);
        seen.Add(general.Name);
        foreach (var f in controls.ToArray())
            if (f is Control c && c.CompileHelp() is { Name.Length: > 0 } info && seen.Add(info.Name))
                infos.Add(info);
        return infos;
    }

    /// <summary>Closes the global help dialog if it is open.</summary>
    public static void HideHelp() => Invoke(() => { if (systemOverlay?.Top is HelpControl) systemOverlay.Hide(); });

    // ---- Focus navigation -------------------------------------------------------------------------------------
    // Two tiers, both on the root layout's 2-D cell grid (Rows/Columns/this[r,c]):
    //   * Ctrl+arrows move spatially BETWEEN root cells (regions), wrapping per axis and skipping cells with no
    //     focusable; landing on a cell focuses its first focusable leaf (descending layouts, frames, composites).
    //   * Ctrl+N/P cycle WITHIN the current region — but only when that region is a multi-focusable nested layout;
    //     a single-control or composite cell is a no-op (enter/leave those with the arrows).

    /// <summary>Moves focus to the next focusable control within the current root-layout region, wrapping. Bound to
    /// <c>Ctrl+N</c> by default.</summary>
    /// <remarks>A no-op unless the focused region is a multi-focusable nested layout.</remarks>
    public static void FocusNext() => CycleFocusWithinRegion(+1);

    /// <summary>Moves focus to the previous focusable control within the current region. Bound to <c>Ctrl+P</c>.</summary>
    public static void FocusPrevious() => CycleFocusWithinRegion(-1);

    /// <summary>Moves focus one cell left/right/up/down in the root layout's 2-D grid (wraps; skips empties). Bound
    /// to <c>Ctrl+Left/Right/Up/Down</c> by default.</summary>
    /// <remarks>Steps one focusable <em>leaf</em>, which is not always one pane: an interactive adornment such as a
    /// <c>SplitPanel</c> divider is a leaf too, so crossing a split takes two presses.</remarks>
    public static void FocusLeft() => MoveSpatialFocus(0, -1);
    /// <summary>Moves focus one cell right in the root layout's 2-D grid. Bound to <c>Ctrl+Right</c> by default.</summary>
    public static void FocusRight() => MoveSpatialFocus(0, +1);
    /// <summary>Moves focus one cell up in the root layout's 2-D grid. Bound to <c>Ctrl+Up</c> by default.</summary>
    public static void FocusUp() => MoveSpatialFocus(-1, 0);
    /// <summary>Moves focus one cell down in the root layout's 2-D grid. Bound to <c>Ctrl+Down</c> by default.</summary>
    public static void FocusDown() => MoveSpatialFocus(+1, 0);

    private static void MoveSpatialFocus(int dRow, int dCol) => Invoke(() =>
    {
        if (layout?.SpatialTarget(dRow, dCol) is { } target) SetFocus(target);
    });

    private static void CycleFocusWithinRegion(int direction) => Invoke(() =>
    {
        if (layout?.RegionCycleTarget(direction, Focused) is { } target) SetFocus(target);
    });

    /// <summary>
    /// Registers an application-wide hotkey handled <em>before</em> any control (so it works regardless of focus),
    /// overwriting any existing action for the same key.
    /// </summary>
    /// <remarks>
    /// <para><see cref="HotKeys.CtrlQ"/> → <see cref="Stop"/> is registered by default. Typically called before
    /// <see cref="Start"/>; the key must match what the input decoder produces (use the <see cref="HotKeys"/>
    /// constants/helpers, e.g. <see cref="HotKeys.Char(char)"/> for a bare letter).</para>
    /// <para><b>Scope:</b> the hotkey table is <b>process-global</b> — one table shared across the whole app, not
    /// scoped to a <see cref="Start"/> root or a control tree. Two consequences worth knowing:</para>
    /// <list type="bullet">
    /// <item>A matched hotkey is dispatched <em>before</em> the focused control and marks the event handled, so a
    /// letter registered as a hotkey never reaches a focused text field. To let a <c>TextInput</c>/<c>TextEditor</c>
    /// receive those letters, <see cref="UnregisterHotKey"/> them while it has focus and re-register on blur.</item>
    /// <item>Constructing a second app/control tree (e.g. a fresh instance per headless snapshot capture) re-registers
    /// the same keys onto the one table, so input then routes to the newest registrant. In tests, register/exercise
    /// one instance at a time (or unregister between them).</item>
    /// </list>
    /// </remarks>
    public static void RegisterHotKey(ConsoleKeyInfo key, Action action) => Invoke(() => GlobalHotKeys[key] = action);

    /// <summary>Removes a global hotkey registered via <see cref="RegisterHotKey"/>.</summary>
    public static void UnregisterHotKey(ConsoleKeyInfo key) => Invoke(() => GlobalHotKeys.Remove(key));

    /// <summary>Drops every hotkey registered via <see cref="RegisterHotKey"/>, leaving only the library's own
    /// (Ctrl+Q, the Ctrl focus tier, F1).</summary>
    /// <remarks>
    /// Called by <see cref="Stop"/>, so an app's hotkeys are scoped to the session that registered them. That matters
    /// when one process starts the UI more than once — swapping between two screens, say: without it the second
    /// session inherits the first's bindings, still pointing at the first session's objects, which by then are
    /// disposed. Register hotkeys per session, alongside the tree they act on.
    /// </remarks>
    public static void RestoreBuiltInHotKeys() => Invoke(() =>
    {
        GlobalHotKeys.Clear();
        foreach (var (key, action) in BuiltInHotKeys) GlobalHotKeys[key] = action;
    });

    /// <summary>
    /// Marks the UI as needing a redraw on the next frame.
    /// </summary>
    /// <remarks>Called whenever control content or layout changes; idle frames skip the redraw until this is set.</remarks>
    public static void MarkDirty() => needsDraw = true;

    /// <summary>The UI thread dispatcher.</summary>
    public static Dispatcher Dispatcher => dispatcher;

    /// <summary>Returns <see langword="true"/> when the caller is on the UI thread (or none is running).</summary>
    public static bool CheckAccess() => dispatcher.CheckAccess();

    /// <summary>Throws when the caller is not on the UI thread.</summary>
    public static void VerifyAccess() => dispatcher.VerifyAccess();

    /// <summary>
    /// Queues <paramref name="action"/> to run on the UI thread on a later turn of the frame loop, and returns
    /// immediately (never blocks, never runs inline).
    /// </summary>
    /// <remarks>
    /// <para>Unlike <see cref="Invoke"/>, it <em>always</em> defers — even when the caller is already on the UI
    /// thread — so the action runs after the current work/layout/paint settles (work posted while the queue is
    /// draining waits for the next frame; see <see cref="Dispatcher"/>).</para>
    /// <para>Use this — rather than <see cref="Invoke"/> — when you specifically want to defer: to break re-entrancy (e.g.
    /// run something <em>after</em> the current input/layout pass), or for a self-feeding pump that must not starve
    /// rendering. It is the low-level primitive: it does NOT request a redraw, so if the action changes visual state
    /// it must invalidate itself (or call <see cref="MarkDirty"/>). For "run on the UI thread, now if I already am,"
    /// use <see cref="Invoke"/> instead.</para>
    /// </remarks>
    public static void Post(Action action) => dispatcher.Post(action);

    /// <summary>Runs an action on the UI thread and returns a task that completes when it finishes.</summary>
    public static Task InvokeAsync(Action action) => dispatcher.InvokeAsync(action);

    /// <summary>Runs a function on the UI thread and returns a task with its result.</summary>
    public static Task<T> InvokeAsync<T>(Func<T> func) => dispatcher.InvokeAsync(func);

    /// <summary>Runs an async delegate on the UI thread and returns a task that completes (unwrapped) when it finishes.</summary>
    public static Task InvokeAsync(Func<Task> func) => dispatcher.InvokeAsync(func);

    /// <summary>Runs an async function on the UI thread and returns a task with its (unwrapped) result.</summary>
    public static Task<T> InvokeAsync<T>(Func<Task<T>> func) => dispatcher.InvokeAsync(func);

    /// <summary>
    /// Synchronously paints a single frame: fires the <see cref="Paint"/> event so every control renders
    /// into its buffer. Intended for headless/snapshot rendering when the UI timer loop is not running.
    /// </summary>
    /// <remarks>
    /// This does not draw to a real console; callers compose the painted control buffers themselves
    /// (for example via a <see cref="ConsoleGUI.Common.DrawingContext"/>).
    /// </remarks>
    public static void PaintFrame() => _Paint?.Invoke(null, paintEventArgs);

    /// <summary>
    /// Sends a key to <paramref name="target"/>. When <paramref name="routeGlobal"/> is <see langword="true"/>,
    /// the global hotkey dispatch runs first (mirroring the live <see cref="OnInput"/> path): a matching hotkey
    /// registered via <see cref="RegisterHotKey"/> fires and marks the event handled, in which case the focused
    /// control never sees the key. Lets headless/snapshot tests exercise global hotkeys, not just control-routed
    /// input. To match a registered hotkey, build <paramref name="key"/> the same way it was registered (e.g. with
    /// the <see cref="HotKeys"/> helpers, or a <see cref="ConsoleKeyInfo"/> whose char matches).
    /// </summary>
    public static void SendInput(IFocusable target, ConsoleKeyInfo key, bool routeGlobal = false)
    {
        var inputEvent = new InputEvent(key);
        if (routeGlobal)
        {
            globalInputListener.OnInput(inputEvent);
            if (inputEvent.Handled) return;
        }
        target.FocusableControl.OnInput(new InputEventArgs(inputEvent));
    }

    /// <summary>
    /// Sends a key (with optional modifiers) to <paramref name="target"/>. See
    /// <see cref="SendInput(IFocusable, ConsoleKeyInfo, bool)"/> for <paramref name="routeGlobal"/>.
    /// </summary>
    public static void SendInput(IFocusable target, ConsoleKey key, bool shift = false, bool alt = false, bool control = false, bool routeGlobal = false)
        => SendInput(target, new ConsoleKeyInfo('\0', key, shift, alt, control), routeGlobal);

    /// <summary>
    /// Runs once per frame on the UI thread: redraws the UI and invokes the <see cref="Paint"/> event,
    /// if the lock is available. (The lock still guards against concurrent background-thread mutation.)
    /// </summary>
    private static void OnFrame()
    {
        // Runs on the UI thread after the dispatcher queue (mutations + input) has been drained, so all
        // layout/geometry changes for this frame have already been applied on this same thread.
        // Bracket the draw/paint cycle to measure its real cost directly: high-resolution wall time (immune to the
        // ~15 ms coarseness of process CPU time) and the managed-heap allocation it caused. Also track the frame
        // period for UI-thread utilisation. ProcessMetrics reads these as peaks so a burst (e.g. a paste) shows.
        long frameStart = Stopwatch.GetTimestamp();
        double periodMs = lastFrameStart != 0 ? (frameStart - lastFrameStart) * 1000.0 / Stopwatch.Frequency : 0;
        lastFrameStart = frameStart;
        long allocBefore = GC.GetTotalAllocatedBytes();
        frameTimer.Restart();

        bool drew = false;
        double dirtyFraction = 0;
        // Number this frame before anything can emit, so a terminal write started during the composite below carries
        // the right ordinal home — the write completes long after this loop has moved on.
        long ordinal = ++frameOrdinal;
        ConsoleManager.FrameOrdinal = ordinal;
        try
        {
            // 1. Detect a terminal resize BEFORE painting, so controls re-lay-out at the new size and repaint into
            //    correctly-sized buffers this same frame (a resize marks the whole surface dirty via Initialize).
            //    Runs every frame (cheap when the size is unchanged); also ticks the legacy software cursor blink.
            ConsoleManager.AdjustBufferSize();

            // A redraw requested BEFORE this frame's paint (from input, a timer, or a structural change drained off
            // the dispatcher queue just now) should be fully satisfied this frame. Captured here so it can be told
            // apart from a redraw requested DURING the paint below — e.g. a control's throttled self-refresh
            // (StatusBar/PerfHud) invalidating from its own paint handler, whose actual repaint bubbles next frame.
            bool redrawRequestedBeforePaint = needsDraw;

            // 2. Paint: controls render into their own buffers and report their damaged screen rects into the
            //    ConsoleManager dirty accumulator. This MUST run before compositing so the composite reads fresh
            //    buffers, not last frame's (or, on the first frame after startup/resize, empty ones) — otherwise
            //    the newly-painted panes show blank until the next full redraw.
            paintTimer.Restart();
            _Paint?.Invoke(null, paintEventArgs);
            paintTimer.Stop();
            // Fractional milliseconds: paints are typically sub-millisecond, which ElapsedMilliseconds truncates to 0.
            paintTimes[paintTimeIndex] = paintTimer.Elapsed.TotalMilliseconds;
            paintTimeIndex = (paintTimeIndex + 1) % paintTimeSamples;

            // 3. Composite the damaged region(s): the whole screen on a full-dirty (startup/resize), else just the
            //    dirty rects reported by the paints above. An idle frame (nothing dirtied) skips the scan and only
            //    keeps a self-blinking ANSI cursor ticking.
            if (needsDraw || ConsoleManager.HasDirty)
            {
                // Safety net: a redraw was requested before this frame's paint, yet the paint localized no damage
                // (HasDirty is false) and nothing marked the surface fully dirty — so the change came from a source
                // that can't be scoped to a rect (e.g. a self-drawing popup opening a submenu). Re-composite
                // everything rather than leave a stale region. A needsDraw raised DURING the paint (a deferred
                // self-refresh) is NOT promoted — its real partial redraw lands next frame — which is what kept the
                // status-bar tick from repainting the whole screen.
                if (redrawRequestedBeforePaint && !ConsoleManager.HasDirty) ConsoleManager.MarkFullDirty();
                needsDraw = false;
                drew = true;
                ConsoleManager.Draw();
                var sz = ConsoleManager.BufferSize;
                long area = (long)sz.Width * sz.Height;
                if (area > 0) dirtyFraction = ConsoleManager.LastFrameDirtyCells / (double)area;
            }
            else
            {
                ConsoleManager.TickCursorBlink();
            }
        }
        finally
        {
            // Record the frame even if the draw/paint threw, so the metrics keep working (and exceptions/s surfaces
            // a per-frame throw) instead of silently freezing at 0.
            frameTimer.Stop();
            ProcessMetrics.RecordFrame(frameTimer.Elapsed.TotalMilliseconds, periodMs, GC.GetTotalAllocatedBytes() - allocBefore, drew, dirtyFraction, ordinal);
        }
    }
       
    /// <summary>
    /// Runs <paramref name="action"/> on the UI thread, marshaling automatically: <em>inline and immediately</em>
    /// when the caller is already on the UI thread (or no UI thread is running, e.g. headless/initialization),
    /// otherwise posted to the dispatcher queue. Then requests a redraw.
    /// </summary>
    /// <remarks>
    /// <para>This is the primary, default way to mutate
    /// control/layout state from anywhere — the change always ends up serialized on the UI thread with rendering, so
    /// no lock is needed.</para>
    /// <para>Does NOT block: off-thread it is fire-and-forget (the action runs later on the UI thread) — it does not wait
    /// for completion or surface the action's exception to the caller. That is unlike the blocking, WPF-style
    /// <see cref="Dispatcher.Invoke(Action)"/>; if you need to wait for the result use <see cref="InvokeAsync(Action)"/>.
    /// Differs from <see cref="Post"/> in two ways: (1) it runs inline when already on the UI thread instead of
    /// always deferring to a later frame, and (2) it requests a redraw afterwards. Prefer this for state changes;
    /// reach for <see cref="Post"/> only when you deliberately want to defer to a later frame.</para>
    /// </remarks>
    /// <param name="action">The action to execute on the UI thread.</param>
    public static void Invoke(Action action)
    {
        // Auto-marshal: run inline when already on the UI thread (or when no UI thread is running, e.g.
        // headless/initialization), otherwise post the mutation to the UI thread. Layout/geometry changes
        // therefore always run on the UI thread, serialized with rendering — no lock required.
        if (CheckAccess())
        {
            action();
        }
        else
        {
            dispatcher.Post(action);
        }
        // Invoke is only called when control/layout state actually changes, so request a redraw.
        needsDraw = true;
    }
    #endregion

    #region Properties
    /// <summary>The root layout hosting the UI's controls, set by <see cref="Start"/>.</summary>
    /// <remarks>Read-only: the root can't be swapped after <see cref="Start"/>. To reconfigure the UI at runtime
    /// (e.g. a full-screen "zen" toggle), change a container in place rather than the root — collapse a pane via
    /// <see cref="SplitPanel.SplitPosition"/>, or reassign a <see cref="DockPanel.DockedControl"/> /
    /// <see cref="DockPanel.FillControl"/>. Move focus to a still-visible control when you hide the focused one.</remarks>
    public static ILayout Layout => layout!;

    /// <summary>
    /// The root overlay host, available once <see cref="Start"/> has run; <see langword="null"/> before <see cref="Start"/>.
    /// </summary>
    /// <remarks>
    /// <para><see cref="Start"/> wraps the app's root in this overlay (reusing it if the root already is an
    /// <see cref="Overlay"/>), so there is always a layer for pop-ups (dropdowns, menus, autocomplete, modals).
    /// Controls that show pop-ups — <see cref="Select"/>, <see cref="MenuBar"/>, <see cref="ContextMenu"/>,
    /// <see cref="Autocomplete"/> — show into this automatically, so there is no per-control overlay to wire up.</para>
    /// <para>Normally you never set this — <see cref="Start"/> does. The setter exists for advanced hosting (and
    /// tests) that need to designate the overlay pop-ups show into without going through <see cref="Start"/>; the
    /// value must be the overlay that is actually being rendered as the root, or pop-ups won't be visible.</para>
    /// </remarks>
    public static Overlay? Overlay
    {
        get => systemOverlay;
        set => systemOverlay = value;
    }

    /// <summary>The mouse button of the most recent press, latched until the next press.</summary>
    /// <remarks>A control's <c>OnClick</c>/<c>OnDoubleClick</c> reads this to distinguish a right-click (e.g. to open
    /// a context menu) from a left-click — the dispatch itself carries only a position, not a button.</remarks>
    public static TerminalMouseButton MouseButton { get; internal set; }

    /// <summary>
    /// The active style theme. Defaults to <see cref="DefaultStyleTheme"/>.
    /// </summary>
    /// <remarks>
    /// Controls capture their default colours/decorations from it. Assigning raises <see cref="ThemeChanged"/>
    /// (on the UI thread), so every live control re-captures — i.e. assigning it is a runtime theme switch.
    /// </remarks>
    public static IStyleTheme StyleTheme
    {
        get => _styleTheme;
        set => Invoke(() => { _styleTheme = value; ThemeChanged?.Invoke(null, EventArgs.Empty); });
    }

    /// <summary>
    /// The active glyph theme. Defaults to <see cref="DefaultGlyphTheme"/>.
    /// </summary>
    /// <remarks>
    /// Controls capture their indicator glyphs from it. Assigning raises <see cref="ThemeChanged"/>
    /// (on the UI thread), so every live control re-captures — i.e. assigning it is a runtime theme switch.
    /// </remarks>
    public static IGlyphTheme GlyphTheme
    {
        get => _glyphTheme;
        set => Invoke(() => { _glyphTheme = value; ThemeChanged?.Invoke(null, EventArgs.Empty); });
    }

    /// <summary>
    /// Convenience to set both themes at once.
    /// </summary>
    /// <remarks>
    /// <para>The work (and the <see cref="ThemeChanged"/> notification that makes live controls re-capture) is done
    /// by the <see cref="StyleTheme"/>/<see cref="GlyphTheme"/> setters.</para>
    /// <para>Re-capture happens only on assignment, never on the render path, so frame-to-frame cost is unchanged.
    /// Each control re-applies the theme only to properties it has not explicitly overridden (see
    /// <c>ThemeOverrides</c>), so explicit per-control styling — including a frame's border — survives the switch.</para>
    /// </remarks>
    public static void SetTheme(IStyleTheme styleTheme, IGlyphTheme glyphTheme)
    {
        StyleTheme = styleTheme;
        GlyphTheme = glyphTheme;
    }

    /// <summary>Applies both halves of a bundled <see cref="ITheme"/> (see <see cref="SetTheme(IStyleTheme, IGlyphTheme)"/>).</summary>
    public static void SetTheme(ITheme theme) => SetTheme(theme.Styles, theme.Glyphs);

    /// <summary>True while the UI loop is running.</summary>
    /// <remarks>Background work (e.g. a Spectre progress/live loop) can poll this to exit when the UI stops.</remarks>
    public static bool IsRunning => isRunning;

    /// <summary>Whether the terminal window currently has focus (DEC mode 1004). Defaults to <see langword="true"/>.</summary>
    /// <remarks>Updated from <see cref="FocusInputEvent"/>s once focus reporting is enabled by the raw input source.</remarks>
    public static bool HasFocus { get; private set; } = true;

    /// <summary>A token that is cancelled when the UI stops.</summary>
    /// <remarks>Background work started alongside the UI (e.g. a Spectre progress/live loop) should observe this so
    /// it terminates on shutdown instead of running on.</remarks>
    public static CancellationToken CancellationToken => cancellationToken;
    
    /// <summary>Average time (ms) spent firing control <see cref="Paint"/> handlers, over the recent sample window.</summary>
    public static double AveragePaintTime
    {
        get
        {
            double total = 0;
            int count = 0;
            foreach (var time in paintTimes)
            {
                if (time > 0)
                {
                    total += time;
                    count++;
                }
            }
            return count > 0 ? total / count : 0;
        }
    }

    /// <summary>Average time (ms) the renderer spent compositing/drawing frames to the console.</summary>
    public static double AverageDrawTime => ConsoleManager.AverageDrawTime;

    /// <summary>
    /// Average time (ms) the actual terminal write took. Concurrent with the next frame's render, so it does NOT add
    /// to <see cref="AverageDrawTime"/> — the throughput ceiling is whichever of the two is larger.
    /// </summary>
    public static double AverageWriteTime => ConsoleManager.AverageOutputWriteTime;

    /// <summary>Average time (ms) a frame waited for the previous frame's write before its own could start.</summary>
    public static double AverageWriteWaitTime => ConsoleManager.AverageOutputWaitTime;

    /// <summary>Frames rendered but not yet written to the terminal, right now — usually 0, since the queue drains
    /// between frames. <see cref="WriteQueueDepthPeak"/> is the one that shows a backlog.</summary>
    public static int WriteQueueDepth => ConsoleManager.OutputQueueDepth;

    /// <summary>
    /// Average end-to-end time (ms) from starting to render a frame to it being written to the terminal: the render,
    /// the wait for the previous write, and the write itself.
    /// </summary>
    /// <remarks>
    /// This is LATENCY, not throughput. The write runs concurrent with the next frame's render, so the frame rate is
    /// bounded by whichever side is slower, not by this sum — a latency of 3 ms does not mean a 333 fps ceiling.
    /// Summed per frame, not from separate averages, so it describes real frames rather than a blend of populations.
    /// </remarks>
    public static double AverageFrameLatency => ProcessMetrics.FrameLatencyMsAvg;

    /// <summary>The worst end-to-end frame latency (ms) in the recent window.</summary>
    /// <remarks>Available only because render and write are paired per frame; adding separate averages could never
    /// yield a worst case.</remarks>
    public static double PeakFrameLatency => ProcessMetrics.FrameLatencyMsPeak;

    /// <summary>The most frames left waiting behind a terminal write over the recent window.</summary>
    /// <remarks>Coarse: frames also queue for a thread-pool slot, so a small peak appears even when the write costs
    /// nothing. <see cref="AverageWriteWaitTime"/> is the reliable back-pressure signal — it measures time actually
    /// spent blocked.</remarks>
    public static int WriteQueueDepthPeak => ConsoleManager.OutputQueueDepthPeak;

    /// <summary>Per-control average paint time (ms) over the recent sample window, keyed by control.</summary>
    public static IDictionary<IFocusable, double> AverageControlPaintTimes
    {
        get
        {
            var d = new Dictionary<IFocusable, double>();   
            foreach(var c in controlPaintTimes)
            {
                double total = 0;
                int count = 0;
                foreach (var time in c.Value)
                {
                    if (time.HasValue)
                    {
                        total += time.Value;
                        count++;
                    }
                }
                d[c.Key] = count > 0 ? total / count : 0;
            }
            return d;
        }
    }

    /// <summary>Per-control peak paint time (ms) over the recent sample window, keyed by control.</summary>
    public static IDictionary<IFocusable, double> MaxControlPaintTimes => controlPaintTimes
        .Select(kv => KeyValuePair.Create(kv.Key, kv.Value.Where(v => v.HasValue).Select(v => v!.Value).DefaultIfEmpty().Max()))
        .ToDictionary();
    #endregion

    #region Events
    /// <summary>Raised by <see cref="SetTheme(IStyleTheme, IGlyphTheme)"/> after the active themes change, so live controls re-apply them.</summary>
    /// <remarks>Controls subscribe in their constructor and unsubscribe on <see cref="IDisposable.Dispose"/>.</remarks>
    public static event EventHandler? ThemeChanged;

    /// <summary>Raised whenever any control's focus changes, so a framed <see cref="CompositeControl"/> can update its
    /// border cue when focus moves into or out of a descendant (its own <see cref="ControlFrame.IsFocused"/> doesn't
    /// change in that case). Frames subscribe/unsubscribe via <see cref="Control.Frame"/>, mirroring
    /// <see cref="ThemeChanged"/>. Fired on the UI thread from <see cref="Control.IsFocused"/>.</summary>
    internal static event Action? FocusChanged;
    internal static void RaiseFocusChanged() => FocusChanged?.Invoke();

    private static EventHandler<PaintEventArgs>? _Paint;
    /// <summary>Raised each frame so subscribed controls render their state; the subscriber's target control is
    /// tracked for per-control paint timing and focus.</summary>
    public static event EventHandler<PaintEventArgs> Paint
    {
        add
        {
            // Track a CONTROL subscriber once for per-control paint timing/focus (a control may add more than one Paint
            // handler — e.g. PerfHud adds base Control.OnPaint and its own OnHudPaint — so the tracking is gated on
            // "not already tracked" while the combine below is not).
            if (value.Target is IFocusable c && !controls.Contains(c))
            {
                controls.Add(c);
                controlPaintTimers[c] = new Stopwatch();
                controlPaintTimes[c] = new double?[paintTimeSamples];
            }
            // ALWAYS combine the handler — including a non-control subscriber (an app-level lambda, an fps counter, a
            // periodic hook). Paint is a public per-frame event; gating the combine on IFocusable silently dropped
            // such handlers so they never fired. Only the per-control bookkeeping above is control-specific.
            _Paint = (EventHandler<PaintEventArgs>?)Delegate.Combine(_Paint, value);
        }
        remove
        {
            _Paint = (EventHandler<PaintEventArgs>?)Delegate.Remove(_Paint, value);
            if (value.Target is IFocusable c)
            {
                // Untrack the control only once none of its Paint handlers remain (a control may have added several).
                bool stillSubscribed = _Paint is not null && _Paint.GetInvocationList().Any(d => ReferenceEquals(d.Target, c));
                if (!stillSubscribed)
                {
                    controls.Remove(c);
                    controlPaintTimers.Remove(c);
                    controlPaintTimes.Remove(c);
                }
            }
        }
    }
    #endregion

    #region Fields
    /// <summary>Lines scrolled per mouse-wheel notch.</summary>
    private const int WheelLines = 3;
    /// <summary>Collector for process/frame performance metrics, sampled each frame and surfaced by the perf HUD.</summary>
    public static readonly ProcessMetrics ProcessMetrics = new ProcessMetrics();
    private static readonly PaintEventArgs paintEventArgs = new PaintEventArgs();
    private static readonly InputEventArgs inputEventArgs = new InputEventArgs();
    private static readonly Dispatcher dispatcher = new Dispatcher();
    private static IStyleTheme _styleTheme = new DefaultStyleTheme();
    private static IGlyphTheme _glyphTheme = new DefaultGlyphTheme();
    private static IInputSource inputSource = new ConsoleInputSource();
    private static Thread? inputThread;
    private static AlternateScreen? altScreen;   // alternate-screen session (ANSI interactive only), restored on Stop
    // Mouse (all encodings) + bracketed-paste + focus reporting OFF, reset SGR, show cursor. Emitted once at Start to
    // clear terminal state a hard-killed previous run couldn't restore. See the self-heal note in Start.
    // Autowrap is healed back ON here too: a hard-killed previous run never ran RestoreWrap, and a shell left with
    // DECAWM off truncates long lines instead of wrapping them. Start turns it off again a few lines later.
    private const string TerminalSelfHealSeq =
        "\x1b[?1000l\x1b[?1002l\x1b[?1003l\x1b[?1006l\x1b[?1015l\x1b[?1016l\x1b[?2004l\x1b[?1004l\x1b[?7h\x1b[0m\x1b[?25h";

    // DECAWM. Off for the session so writing the bottom-right cell cannot scroll the screen; see the note in Start.
    private const string WrapOffSeq = "\x1b[?7l";
    private const string WrapOnSeq = "\x1b[?7h";
    private static bool wrapDisabled;
    private static List<PosixSignalRegistration>? signalRegistrations;
    private static TaskCompletionSource runCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    private static CancellationTokenSource cts = new CancellationTokenSource();
    private static CancellationToken cancellationToken = cts.Token;
    private static int interval = 100;
    private static bool isRunning;
    private static volatile bool needsDraw = true;
    private static ILayout? layout;
    // UI-owned overlay wrapping the app root; hosts global modals (the F1 help dialog). Set in Start.
    private static Overlay? systemOverlay;
    private static readonly List<IFocusable> controls = new List<IFocusable>();
    private static readonly GlobalInputListener globalInputListener = new GlobalInputListener();
    // The hotkeys the library owns. Separate from the live table because Stop restores it: an app's own hotkeys are
    // session state, and a second Start must not inherit the first session's bindings (see RestoreBuiltInHotKeys).
    private static readonly KeyValuePair<ConsoleKeyInfo, Action>[] BuiltInHotKeys =
    [
        new(HotKeys.CtrlQ, Stop),
        // Focus navigation (the "Ctrl tier"). Ctrl+arrows move spatially between root-layout regions; Ctrl+N/P
        // cycle within the current region. Plain keys stay with the focused control; Alt is layout-specific nav.
        new(HotKeys.CtrlN, FocusNext),
        new(HotKeys.CtrlP, FocusPrevious),
        new(HotKeys.CtrlLeft, FocusLeft),
        new(HotKeys.CtrlRight, FocusRight),
        new(HotKeys.CtrlUp, FocusUp),
        new(HotKeys.CtrlDown, FocusDown),
        // Global help dialog (toggles open/closed).
        new(HotKeys.F1, ShowHelp),
    ];

    private static readonly Dictionary<ConsoleKeyInfo, Action> GlobalHotKeys = new(BuiltInHotKeys);
    // Monotonic frame number, the join key between a frame's render cost (recorded here, on the UI thread) and its
    // terminal write (recorded later, on a pool thread). UI-thread only.
    private static long frameOrdinal;
    private static readonly int paintTimeSamples = 60;
    private static readonly double[] paintTimes = new double[paintTimeSamples];
    private static readonly Stopwatch paintTimer = new Stopwatch();
    // Measures the whole draw/paint cycle each frame (fed to ProcessMetrics.RecordFrame); lastFrameStart tracks the
    // frame period for UI-thread utilisation.
    private static readonly Stopwatch frameTimer = new Stopwatch();
    private static long lastFrameStart;
    internal static int paintTimeIndex = 0;
    internal static readonly Dictionary<IFocusable, Stopwatch> controlPaintTimers = new();
    // Fractional milliseconds per control per frame (null when the control didn't repaint that frame). Fractional,
    // not whole ms — most controls (buttons, tab items, labels) repaint in well under 1 ms and would otherwise all
    // record 0, biasing every per-control average/peak to zero.
    internal static readonly Dictionary<IFocusable, double?[]> controlPaintTimes = new();
    private static readonly PerfHud perfHud = new PerfHud();
    #endregion

    #region Types
    /// <summary>Arguments for the <see cref="Paint"/> event; carries no data (controls read their own state).</summary>
    public class PaintEventArgs : EventArgs
    {
    }

    /// <summary>Arguments for control input handling, wrapping the decoded <see cref="InputEvent"/>.</summary>
    public class InputEventArgs : EventArgs
    {
        /// <summary>The decoded input event being dispatched, or <see langword="null"/>.</summary>
        public InputEvent? InputEvent { get; internal set; }

        /// <summary>Initializes an empty <see cref="InputEventArgs"/>.</summary>
        public InputEventArgs()
        {
        }

        /// <summary>Initializes a new <see cref="InputEventArgs"/> carrying <paramref name="inputEvent"/>.</summary>
        public InputEventArgs(InputEvent? inputEvent)
        {
            InputEvent = inputEvent;
        }
    }

    /// <summary>Input listener that dispatches globally-registered hotkeys before any control sees the event.</summary>
    public class GlobalInputListener: IInputListener
    {
        /// <summary>Invokes the registered action for a matching global hotkey and marks the event handled.</summary>
        public void OnInput(InputEvent inputEvent)
        {
            if (GlobalHotKeys.TryGetValue(inputEvent.Key, out var action))
            {               
                action?.Invoke();
                inputEvent.Handled = true;
                return;                
            }           
        }
    }

    /// <summary>Factory helpers and well-known <see cref="ConsoleKeyInfo"/> constants for registering hotkeys.</summary>
    public static class HotKeys
    {
        /// <summary>Builds a <see cref="ConsoleKeyInfo"/> for <paramref name="key"/> with the Ctrl modifier.</summary>
        public static ConsoleKeyInfo Ctrl(ConsoleKey key)
        {
            // For letter keys, a control character is generated. For other keys, the character is '\0'.
            char keyChar = (key >= ConsoleKey.A && key <= ConsoleKey.Z)
                ? (char)(char.ToLower((char)key) - 96)
                : '\0';
            return new ConsoleKeyInfo(keyChar, key, false, false, true);
        }

        /// <summary>Builds a <see cref="ConsoleKeyInfo"/> for <paramref name="key"/> with the Alt modifier.</summary>
        public static ConsoleKeyInfo Alt(ConsoleKey key)
        {
            // For letter keys, a lowercase character is generated. For other keys, the character is '\0'.
            char keyChar = (key >= ConsoleKey.A && key <= ConsoleKey.Z)
                ? char.ToLower((char)key)
                : '\0';
            return new ConsoleKeyInfo(keyChar, key, false, true, false);
        }

        /// <summary>Builds a <see cref="ConsoleKeyInfo"/> for <paramref name="key"/> with both Ctrl and Alt modifiers.</summary>
        public static ConsoleKeyInfo CtrlAlt(ConsoleKey key) =>
            new('\0', key, false, true, true);

        /// <summary>Builds a <see cref="ConsoleKeyInfo"/> for <paramref name="key"/> with the Shift modifier — for
        /// Shift+arrow / Shift+PageUp and similar non-letter combos (letter keys carry their uppercase char, matching
        /// the input decoder). For a Shift+letter as a printable character, prefer <see cref="Char(char)"/>.</summary>
        public static ConsoleKeyInfo Shift(ConsoleKey key)
        {
            // Letter keys carry the uppercase char (ConsoleKey.A..Z are 'A'..'Z'); other keys use '\0'.
            char keyChar = (key >= ConsoleKey.A && key <= ConsoleKey.Z) ? (char)key : '\0';
            return new ConsoleKeyInfo(keyChar, key, true, false, false);
        }

        /// <summary>Builds a <see cref="ConsoleKeyInfo"/> for a bare printable key — a letter, digit, punctuation,
        /// or space — so it can be registered as a global hotkey (e.g. <c>UI.RegisterHotKey(UI.HotKeys.Char('q'),
        /// UI.Stop)</c>).</summary>
        /// <remarks>Produces exactly what the input decoder emits for that keystroke, so the registered hotkey
        /// matches a real press: an uppercase letter carries Shift; every non-letter/digit (e.g. <c>'/'</c>) uses
        /// key code <c>0</c> with the character. The same value drives a headless test — pass
        /// <c>UI.HotKeys.Char(c)</c> to the Snapshot package's <c>ToTextAfter(..., routeGlobal: true)</c>.</remarks>
        public static ConsoleKeyInfo Char(char c)
        {
            // Mirror AnsiInputDecoder's printable-char mapping exactly, so a registered hotkey matches the live key.
            var (key, shift) = c switch
            {
                >= 'a' and <= 'z' => (ConsoleKey.A + (c - 'a'), false),
                >= 'A' and <= 'Z' => (ConsoleKey.A + (c - 'A'), true),
                >= '0' and <= '9' => (ConsoleKey.D0 + (c - '0'), false),
                ' ' => (ConsoleKey.Spacebar, false),
                _ => ((ConsoleKey)0, false),
            };
            return new ConsoleKeyInfo(c, key, shift, false, false);
        }

        /// <summary>The Escape key, as produced by the input decoder (KeyChar <c>\x1b</c>, no modifiers).</summary>
        public static ConsoleKeyInfo Escape = new('\x1b', ConsoleKey.Escape, false, false, false);

        /// <summary>The Tab key, as produced by the input decoder (KeyChar <c>\t</c>, no modifiers).</summary>
        public static ConsoleKeyInfo Tab = new('\t', ConsoleKey.Tab, false, false, false);

        /// <summary>The F1 key, as produced by the input decoder (SS3 <c>ESC O P</c> → KeyChar <c>\0</c>, no modifiers).</summary>
        public static ConsoleKeyInfo F1 = new('\0', ConsoleKey.F1, false, false, false);

        /// <summary>Shift+Tab (back-tab), as produced by the input decoder from CSI Z (KeyChar <c>\0</c>, Shift).</summary>
        public static ConsoleKeyInfo ShiftTab = new('\0', ConsoleKey.Tab, true, false, false);

        /// <summary>Ctrl+Q — the default quit hotkey.</summary>
        public static ConsoleKeyInfo CtrlQ = Ctrl(ConsoleKey.Q);
        // Focus navigation (Ctrl tier): Ctrl+N/P cycle within a region; Ctrl+arrows move between regions.
        /// <summary>Ctrl+N — cycles focus to the next control within the region.</summary>
        public static ConsoleKeyInfo CtrlN = Ctrl(ConsoleKey.N);
        /// <summary>Ctrl+P — cycles focus to the previous control within the region.</summary>
        public static ConsoleKeyInfo CtrlP = Ctrl(ConsoleKey.P);
        /// <summary>Ctrl+Left — moves focus one region left.</summary>
        public static ConsoleKeyInfo CtrlLeft = Ctrl(ConsoleKey.LeftArrow);
        /// <summary>Ctrl+Right — moves focus one region right.</summary>
        public static ConsoleKeyInfo CtrlRight = Ctrl(ConsoleKey.RightArrow);
        /// <summary>Ctrl+Up — moves focus one region up.</summary>
        public static ConsoleKeyInfo CtrlUp = Ctrl(ConsoleKey.UpArrow);
        /// <summary>Ctrl+Down — moves focus one region down.</summary>
        public static ConsoleKeyInfo CtrlDown = Ctrl(ConsoleKey.DownArrow);
        /// <summary>Ctrl+F12.</summary>
        public static ConsoleKeyInfo CtrlF12 = Ctrl(ConsoleKey.F12);
        // Alt+arrows — the "Alt tier" layout navigation keys (e.g. TabPanel switches tabs on Alt+Left/Right).
        /// <summary>Alt+Up — Alt-tier layout navigation.</summary>
        public static ConsoleKeyInfo AltUp = Alt(ConsoleKey.UpArrow);
        /// <summary>Alt+Down — Alt-tier layout navigation.</summary>
        public static ConsoleKeyInfo AltDown = Alt(ConsoleKey.DownArrow);
        /// <summary>Alt+Left — Alt-tier layout navigation.</summary>
        public static ConsoleKeyInfo AltLeft = Alt(ConsoleKey.LeftArrow);
        /// <summary>Alt+Right — Alt-tier layout navigation.</summary>
        public static ConsoleKeyInfo AltRight = Alt(ConsoleKey.RightArrow);
        /// <summary>Ctrl+Alt+Up.</summary>
        public static ConsoleKeyInfo CtrlAltUp = CtrlAlt(ConsoleKey.UpArrow);
    }
    #endregion
}


