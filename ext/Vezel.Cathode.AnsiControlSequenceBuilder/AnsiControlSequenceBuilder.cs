// SPDX-License-Identifier: 0BSD

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Vezel.Cathode.Diagnostics;
using static Vezel.Cathode.Text.Control.ControlConstants;

namespace Vezel.Cathode.Text.Control;

public sealed class AnsiControlSequenceBuilder
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [InterpolatedStringHandler]
    public readonly ref struct PrintInterpolatedStringHandler
    {
        private const int StackBufferSize = 256;

        private readonly AnsiControlSequenceBuilder _builder;

        private readonly IFormatProvider? _provider;

        private readonly ICustomFormatter? _formatter;

        public PrintInterpolatedStringHandler(
            [SuppressMessage("", "IDE0060")] int literalLength,
            [SuppressMessage("", "IDE0060")] int formattedCount,
            AnsiControlSequenceBuilder builder,
            IFormatProvider? provider = null)
        {
            _builder = builder;
            _provider = provider;
            _formatter =
                provider is not CultureInfo ? (ICustomFormatter?)provider?.GetFormat(typeof(ICustomFormatter)) : null;
        }

        private void AppendSpan(scoped ReadOnlySpan<char> span)
        {
            _ = _builder.Print(span);
        }

        public void AppendLiteral(string value)
        {
            AppendSpan(value);
        }

        [SuppressMessage("", "IDE0038")]
        public void AppendFormatted<T>(T value, string? format = null)
        {
            if (_formatter != null)
            {
                AppendSpan(_formatter.Format(format, value, _provider));

                return;
            }

            // Do not use pattern matching here as it results in boxing.
            if (value is IFormattable)
            {
                if (value is ISpanFormattable)
                {
                    var rented = default(char[]);
                    var span = (stackalloc char[StackBufferSize]);

                    try
                    {
                        int written;

                        // Try to format the value on the stack; fall back to the heap.
                        while (!((ISpanFormattable)value).TryFormat(span, out written, format, _provider))
                        {
                            if (rented != null)
                                ArrayPool<char>.Shared.Return(rented);

                            var len = span.Length * 2;

                            rented = ArrayPool<char>.Shared.Rent(len);
                            span = rented.AsSpan(..len);
                        }

                        AppendSpan(span[..written]);
                    }
                    finally
                    {
                        if (rented != null)
                            ArrayPool<char>.Shared.Return(rented);
                    }
                }
                else
                    AppendSpan(((IFormattable)value).ToString(format, _provider));
            }
            else
                AppendSpan(value?.ToString());
        }

        public void AppendFormatted(object? value, string? format = null)
        {
            // This overload is used when a target-typed expression cannot use the generic overload.
            AppendFormatted<object?>(value, format);
        }

        public void AppendFormatted(string? value)
        {
            // This overload exists to disambiguate string since it can implicitly convert to both object and
            // ReadOnlySpan<char>.
            AppendFormatted<string?>(value);
        }

        /*
        public unsafe void AppendFormatted(void* value, string? format = null)
        {
            // This overload makes pointer values work in interpolation holes; they cannot be passed as generic type
            // arguments currently.
            AppendFormatted((nuint)value, format);
        }
        */
        public void AppendFormatted(scoped ReadOnlySpan<char> value)
        {
            AppendSpan(value);
        }
    }

    private const int StackBufferSize = 32;

    public ReadOnlySpan<char> Span => _writer.WrittenSpan;

    public ReadOnlyMemory<char> Memory => _writer.WrittenMemory;

    private static readonly CultureInfo _culture = CultureInfo.InvariantCulture;

    private readonly int _capacity;

    private ArrayBufferWriter<char> _writer;

    public AnsiControlSequenceBuilder(int capacity = 1024)
    {
        Check.Range(capacity > 0, capacity);

        _capacity = capacity;
        _writer = new(capacity);
    }

    public void Clear(int reallocateThreshold = 4096)
    {
        Check.Range(reallocateThreshold >= 0, reallocateThreshold);

        if (reallocateThreshold != 0 && _writer.Capacity > reallocateThreshold)
            _writer = new(_capacity);
        else
            _writer.Clear();
    }

    public AnsiControlSequenceBuilder Print(scoped ReadOnlySpan<char> value)
    {
        _writer.Write(value);

        return this;
    }

    [SuppressMessage("", "IDE0060")]
    public AnsiControlSequenceBuilder Print(
        [InterpolatedStringHandlerArgument("")] scoped ref PrintInterpolatedStringHandler handler)
    {
        return this;
    }

    [SuppressMessage("", "IDE0060")]
    public AnsiControlSequenceBuilder Print(
        IFormatProvider? provider,
        [InterpolatedStringHandlerArgument("", nameof(provider))] scoped ref PrintInterpolatedStringHandler handler)
    {
        return this;
    }

    public AnsiControlSequenceBuilder PrintLine()
    {
        return Print(Environment.NewLine);
    }

    public AnsiControlSequenceBuilder PrintLine(scoped ReadOnlySpan<char> value)
    {
        return Print(value).PrintLine();
    }

    [SuppressMessage("", "IDE0060")]
    public AnsiControlSequenceBuilder PrintLine(
        [InterpolatedStringHandlerArgument("")] scoped ref PrintInterpolatedStringHandler handler)
    {
        return PrintLine();
    }

    [SuppressMessage("", "IDE0060")]
    public AnsiControlSequenceBuilder PrintLine(
        IFormatProvider? provider,
        [InterpolatedStringHandlerArgument("", nameof(provider))] scoped ref PrintInterpolatedStringHandler handler)
    {
        return PrintLine();
    }

    // Keep methods in sync with the ControlSequences class.

    public AnsiControlSequenceBuilder Null()
    {
        return Print([NUL]);
    }

    public AnsiControlSequenceBuilder Beep()
    {
        return Print([BEL]);
    }

    public AnsiControlSequenceBuilder Backspace()
    {
        return Print([BS]);
    }

    public AnsiControlSequenceBuilder HorizontalTab()
    {
        return Print([HT]);
    }

    public AnsiControlSequenceBuilder LineFeed()
    {
        return Print([LF]);
    }

    public AnsiControlSequenceBuilder VerticalTab()
    {
        return Print([VT]);
    }

    public AnsiControlSequenceBuilder FormFeed()
    {
        return Print([FF]);
    }

    public AnsiControlSequenceBuilder CarriageReturn()
    {
        return Print([CR]);
    }

    public AnsiControlSequenceBuilder Substitute()
    {
        return Print([SUB]);
    }

    public AnsiControlSequenceBuilder Cancel()
    {
        return Print([CAN]);
    }

    public AnsiControlSequenceBuilder FileSeparator()
    {
        return Print([FS]);
    }

    public AnsiControlSequenceBuilder GroupSeparator()
    {
        return Print([GS]);
    }

    public AnsiControlSequenceBuilder RecordSeparator()
    {
        return Print([RS]);
    }

    public AnsiControlSequenceBuilder UnitSeparator()
    {
        return Print([US]);
    }

    public AnsiControlSequenceBuilder Space()
    {
        return Print([SP]);
    }

    public AnsiControlSequenceBuilder SetOutputBatching(bool enable)
    {
        return Print(CSI).Print("?2026").Print(enable ? "h" : "l");
    }

    public AnsiControlSequenceBuilder SetTitle(scoped ReadOnlySpan<char> title)
    {
        return Print(OSC).Print("2;").Print(title).Print(ST);
    }

    public AnsiControlSequenceBuilder PushTitle()
    {
        return Print(CSI).Print("22;2t");
    }

    public AnsiControlSequenceBuilder PopTitle()
    {
        return Print(CSI).Print("23;2t");
    }

    public AnsiControlSequenceBuilder SetProgress(ProgressState state, int value)
    {
        Check.Enum(state);
        Check.Range(Math.Clamp(value, 0, 100) == value, value);

        return Print(_culture, $"{OSC}9;4;{(int)state};{value}{ST}");
    }

    public AnsiControlSequenceBuilder SetCursorKeyMode(CursorKeyMode mode)
    {
        Check.Enum(mode);

        var ch = (char)mode;

        return Print(CSI).Print("?1").Print([ch]);
    }

    public AnsiControlSequenceBuilder SetKeypadMode(KeypadMode mode)
    {
        Check.Enum(mode);

        var ch = (char)mode;

        return Print([ESC]).Print([ch]);
    }

    public AnsiControlSequenceBuilder SetKeyboardLevel(KeyboardLevel level)
    {
        var (cursor, function, other) = level switch
        {
            KeyboardLevel.Basic => ("1n", "2n", "4n"),
            KeyboardLevel.Normal => ("1;2m", "2;2m", "4;0m"),
            KeyboardLevel.Extended => ("1;2m", "2;2m", "4;2m"),
            _ => throw new ArgumentOutOfRangeException(nameof(level)),
        };

        return Print(CSI).Print(cursor).Print(CSI).Print(function).Print(CSI).Print(other);
    }

    public AnsiControlSequenceBuilder SetAutoRepeatMode(bool enable)
    {
        return Print(CSI).Print("?8").Print(enable ? "h" : "l");
    }

    public AnsiControlSequenceBuilder SetMouseEvents(MouseEvents events)
    {
        return Print(CSI).Print("?1003").Print(events.HasFlag(MouseEvents.Movement) ? "h" : "l")
            .Print(CSI).Print("?1006").Print(events.HasFlag(MouseEvents.Buttons) ? "h" : "l");
    }

    public AnsiControlSequenceBuilder SetMousePointerStyle(scoped ReadOnlySpan<char> style)
    {
        return Print(OSC).Print("22;").Print(style).Print(ST);
    }

    public AnsiControlSequenceBuilder SetFocusEvents(bool enable)
    {
        return Print(CSI).Print("?1004").Print(enable ? "h" : "l");
    }

    public AnsiControlSequenceBuilder SetBracketedPaste(bool enable)
    {
        return Print(CSI).Print("?2004").Print(enable ? "h" : "l");
    }

    public AnsiControlSequenceBuilder SetScreenBuffer(ScreenBuffer buffer)
    {
        Check.Enum(buffer);

        var ch = (char)buffer;

        return Print(CSI).Print("?1049").Print([ch]);
    }

    public AnsiControlSequenceBuilder SetInvertedColors(bool enable)
    {
        return Print(CSI).Print("?5").Print(enable ? "h" : "l");
    }

    public AnsiControlSequenceBuilder SetCursorVisibility(bool visible)
    {
        return Print(CSI).Print("?25").Print(visible ? "h" : "l");
    }

    public AnsiControlSequenceBuilder SetCursorStyle(CursorStyle style)
    {
        Check.Enum(style);

        return Print(_culture, $"{CSI}{(int)style} q");
    }

    public AnsiControlSequenceBuilder SetScrollBarVisibility(bool visible)
    {
        return Print(CSI).Print("?30").Print(visible ? "h" : "l");
    }

    public AnsiControlSequenceBuilder SetScrollMargin(int top, int bottom)
    {
        Check.Range(top >= 0, top);
        Check.Range(bottom > top, bottom);

        return Print(_culture, $"{CSI}{top + 1};{bottom + 1}r");
    }

    public AnsiControlSequenceBuilder ResetScrollMargin()
    {
        return Print(CSI).Print(";r");
    }

    private AnsiControlSequenceBuilder ModifyText(string type, int count)
    {
        Check.Range(count >= 0, count);

        if (count == 0)
            return this;

        return Print(_culture, $"{CSI}{count}{type}");
    }

    public AnsiControlSequenceBuilder InsertCharacters(int count)
    {
        return ModifyText("@", count);
    }

    public AnsiControlSequenceBuilder DeleteCharacters(int count)
    {
        return ModifyText("P", count);
    }

    public AnsiControlSequenceBuilder EraseCharacters(int count)
    {
        return ModifyText("X", count);
    }

    public AnsiControlSequenceBuilder InsertLines(int count)
    {
        return ModifyText("L", count);
    }

    public AnsiControlSequenceBuilder DeleteLines(int count)
    {
        return ModifyText("M", count);
    }

    private AnsiControlSequenceBuilder Clear(string type, ClearMode mode)
    {
        Check.Enum(mode);

        return Print(_culture, $"{CSI}{(int)mode}{type}");
    }

    public AnsiControlSequenceBuilder ClearScreen(ClearMode mode = ClearMode.Full)
    {
        return Clear("J", mode);
    }

    public AnsiControlSequenceBuilder ClearLine(ClearMode mode = ClearMode.Full)
    {
        return Clear("K", mode);
    }

    public AnsiControlSequenceBuilder SetProtection(bool protect)
    {
        return Print(CSI).Print(protect ? "1" : "0").Print("\"q");
    }

    private AnsiControlSequenceBuilder ProtectedClear(string type, ClearMode mode)
    {
        Check.Enum(mode);

        return Print(_culture, $"{CSI}?{(int)mode}{type}");
    }

    public AnsiControlSequenceBuilder ProtectedClearScreen(ClearMode mode = ClearMode.Full)
    {
        return ProtectedClear("J", mode);
    }

    public AnsiControlSequenceBuilder ProtectedClearLine(ClearMode mode = ClearMode.Full)
    {
        return ProtectedClear("K", mode);
    }

    private AnsiControlSequenceBuilder MoveBuffer(string type, int count)
    {
        Check.Range(count >= 0, count);

        if (count == 0)
            return this;

        return Print(_culture, $"{CSI}{count}{type}");
    }

    public AnsiControlSequenceBuilder MoveBufferUp(int count)
    {
        return MoveBuffer("S", count);
    }

    public AnsiControlSequenceBuilder MoveBufferDown(int count)
    {
        return MoveBuffer("T", count);
    }

    public AnsiControlSequenceBuilder MoveCursorTo(int line, int column)
    {
        Check.Range(line >= 0, line);
        Check.Range(column >= 0, column);

        return Print(_culture, $"{CSI}{line + 1};{column + 1}H");
    }

    private AnsiControlSequenceBuilder MoveCursor(string type, int count)
    {
        Check.Range(count >= 0, count);

        if (count == 0)
            return this;

        return Print(_culture, $"{CSI}{count}{type}");
    }

    public AnsiControlSequenceBuilder MoveCursorUp(int count)
    {
        return MoveCursor("A", count);
    }

    public AnsiControlSequenceBuilder MoveCursorDown(int count)
    {
        return MoveCursor("B", count);
    }

    public AnsiControlSequenceBuilder MoveCursorLeft(int count)
    {
        return MoveCursor("D", count);
    }

    public AnsiControlSequenceBuilder MoveCursorRight(int count)
    {
        return MoveCursor("C", count);
    }

    public AnsiControlSequenceBuilder SaveCursorState()
    {
        return Print([ESC]).Print("7");
    }

    public AnsiControlSequenceBuilder RestoreCursorState()
    {
        return Print([ESC]).Print("8");
    }

    public AnsiControlSequenceBuilder SetForegroundColor(Color color)
    {
        Check.Argument(color.A == byte.MaxValue, color);

        return Print(_culture, $"{CSI}38;2;{color.R};{color.G};{color.B}m");
    }

    public AnsiControlSequenceBuilder SetBackgroundColor(Color color)
    {
        Check.Argument(color.A == byte.MaxValue, color);

        return Print(_culture, $"{CSI}48;2;{color.R};{color.G};{color.B}m");
    }

    public AnsiControlSequenceBuilder SetUnderlineColor(Color color)
    {
        Check.Argument(color.A == byte.MaxValue, color);

        return Print(_culture, $"{CSI}58;2;{color.R};{color.G};{color.B}m");
    }

    public AnsiControlSequenceBuilder SetDecorations(
        bool intense = false,
        bool faint = false,
        bool italic = false,
        bool underline = false,
        bool curlyUnderline = false,
        bool dottedUnderline = false,
        bool dashedUnderline = false,
        bool blink = false,
        bool rapidBlink = false,
        bool invert = false,
        bool invisible = false,
        bool strikethrough = false,
        bool doubleUnderline = false,
        bool overline = false)
    {
        _ = Print(CSI);

        var i = 0;

        void HandleMode(bool value, scoped ReadOnlySpan<char> code)
        {
            if (!value)
                return;

            if (i != 0)
                _ = Print(";");

            i++;

            _ = Print(code);
        }

        HandleMode(intense, "1");
        HandleMode(faint, "2");
        HandleMode(italic, "3");
        HandleMode(underline, "4");
        HandleMode(curlyUnderline, "4:3");
        HandleMode(dottedUnderline, "4:4");
        HandleMode(dashedUnderline, "4:5");
        HandleMode(blink, "5");
        HandleMode(rapidBlink, "6");
        HandleMode(invert, "7");
        HandleMode(invisible, "8");
        HandleMode(strikethrough, "9");
        HandleMode(doubleUnderline, "21");
        HandleMode(overline, "53");

        return Print("m");
    }

    public AnsiControlSequenceBuilder ResetAttributes()
    {
        return Print(CSI).Print("0m");
    }

    public AnsiControlSequenceBuilder OpenHyperlink(Uri uri, scoped ReadOnlySpan<char> id = default)
    {
        Check.Null(uri);

        _ = Print(OSC).Print("8;");

        if (!id.IsEmpty)
            _ = Print("id=").Print(id);

        return Print(";").Print(uri.ToString()).Print(ST);
    }

    public AnsiControlSequenceBuilder CloseHyperlink()
    {
        return Print(OSC).Print("8;;").Print(ST);
    }

    public AnsiControlSequenceBuilder SetWorkingDirectory(Uri uri)
    {
        Check.Null(uri);
        Check.Argument(uri.Scheme == Uri.UriSchemeFile, uri);

        return Print(OSC).Print("7").Print(uri.ToString()).Print(ST);
    }

    public AnsiControlSequenceBuilder SetWorkingDirectory(scoped ReadOnlySpan<char> path)
    {
        Check.Argument(!path.IsEmpty, path);

        return Print(OSC).Print("9;9;").Print("\"").Print(path).Print("\"").Print(ST);
    }

    public AnsiControlSequenceBuilder BeginShellPrompt()
    {
        return Print(OSC).Print("133;A").Print(ST);
    }

    public AnsiControlSequenceBuilder EndShellPrompt()
    {
        return Print(OSC).Print("133;B").Print(ST);
    }

    public AnsiControlSequenceBuilder BeginShellExecution()
    {
        return Print(OSC).Print("133;C").Print(ST);
    }

    public AnsiControlSequenceBuilder EndShellExecution(int? code = null)
    {
        _ = Print(OSC).Print("133;D");

        if (code is { } c)
        {
            _ = Print(_culture, $";{c}");
        }

        return Print(ST);
    }

    public AnsiControlSequenceBuilder SaveScreenshot(ScreenshotFormat format = ScreenshotFormat.Html)
    {
        Check.Enum(format);

        return Print(_culture, $"{CSI}{(int)format}i");
    }

    public AnsiControlSequenceBuilder PlayNotes(int volume, int duration, scoped ReadOnlySpan<int> notes)
    {
        Check.Range(volume is >= 0 and <= 7, volume);
        Check.Range(duration >= 0, duration);
        Check.Argument(notes.Length >= 1, nameof(notes));
        Check.All(notes, static note => note is >= 1 and <= 25);

        Print(_culture, $"{CSI}{volume};{duration}");

        foreach (var note in notes)
        {
            Print(_culture, $";{note}");
        }

        return Print(",~");
    }

    public AnsiControlSequenceBuilder SoftReset()
    {
        return Print(CSI).Print("!p");
    }

    public AnsiControlSequenceBuilder FullReset()
    {
        return Print([ESC]).Print("c");
    }

    public override string ToString()
    {
        return Span.ToString();
    }

    public void WriteToSystemConsole() => Console.Write(Span);

    public async Task WriteToSystemConsoleAsync()
    {
        var buf = MemoryMarshal.AsBytes(Span);
        using var s = Console.OpenStandardOutput();
        s.Write(buf);
        await s.FlushAsync();        
    }
}