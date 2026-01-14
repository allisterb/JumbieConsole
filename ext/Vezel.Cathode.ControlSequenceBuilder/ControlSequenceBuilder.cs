// SPDX-License-Identifier: 0BSD

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Buffers;
using System.Runtime.CompilerServices;

using Vezel.Cathode.Diagnostics;
using static Vezel.Cathode.Text.Control.ControlConstants;

namespace Vezel.Cathode.Text.Control;

public sealed class ControlSequenceBuilder
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [InterpolatedStringHandler]
    public readonly ref struct PrintInterpolatedStringHandler
    {
        private const int StackBufferSize = 256;

        private readonly ControlSequenceBuilder _builder;

        private readonly IFormatProvider? _provider;

        private readonly ICustomFormatter? _formatter;

        public PrintInterpolatedStringHandler(
            [SuppressMessage("", "IDE0060")] int literalLength,
            [SuppressMessage("", "IDE0060")] int formattedCount,
            ControlSequenceBuilder builder,
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

    public ControlSequenceBuilder(int capacity = 1024)
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

    public ControlSequenceBuilder Print(scoped ReadOnlySpan<char> value)
    {
        _writer.Write(value);

        return this;
    }

    [SuppressMessage("", "IDE0060")]
    public ControlSequenceBuilder Print(
        [InterpolatedStringHandlerArgument("")] scoped ref PrintInterpolatedStringHandler handler)
    {
        return this;
    }

    [SuppressMessage("", "IDE0060")]
    public ControlSequenceBuilder Print(
        IFormatProvider? provider,
        [InterpolatedStringHandlerArgument("", nameof(provider))] scoped ref PrintInterpolatedStringHandler handler)
    {
        return this;
    }

    public ControlSequenceBuilder PrintLine()
    {
        return Print(Environment.NewLine);
    }

    public ControlSequenceBuilder PrintLine(scoped ReadOnlySpan<char> value)
    {
        return Print(value).PrintLine();
    }

    [SuppressMessage("", "IDE0060")]
    public ControlSequenceBuilder PrintLine(
        [InterpolatedStringHandlerArgument("")] scoped ref PrintInterpolatedStringHandler handler)
    {
        return PrintLine();
    }

    [SuppressMessage("", "IDE0060")]
    public ControlSequenceBuilder PrintLine(
        IFormatProvider? provider,
        [InterpolatedStringHandlerArgument("", nameof(provider))] scoped ref PrintInterpolatedStringHandler handler)
    {
        return PrintLine();
    }

    // Keep methods in sync with the ControlSequences class.

    public ControlSequenceBuilder Null()
    {
        return Print([NUL]);
    }

    public ControlSequenceBuilder Beep()
    {
        return Print([BEL]);
    }

    public ControlSequenceBuilder Backspace()
    {
        return Print([BS]);
    }

    public ControlSequenceBuilder HorizontalTab()
    {
        return Print([HT]);
    }

    public ControlSequenceBuilder LineFeed()
    {
        return Print([LF]);
    }

    public ControlSequenceBuilder VerticalTab()
    {
        return Print([VT]);
    }

    public ControlSequenceBuilder FormFeed()
    {
        return Print([FF]);
    }

    public ControlSequenceBuilder CarriageReturn()
    {
        return Print([CR]);
    }

    public ControlSequenceBuilder Substitute()
    {
        return Print([SUB]);
    }

    public ControlSequenceBuilder Cancel()
    {
        return Print([CAN]);
    }

    public ControlSequenceBuilder FileSeparator()
    {
        return Print([FS]);
    }

    public ControlSequenceBuilder GroupSeparator()
    {
        return Print([GS]);
    }

    public ControlSequenceBuilder RecordSeparator()
    {
        return Print([RS]);
    }

    public ControlSequenceBuilder UnitSeparator()
    {
        return Print([US]);
    }

    public ControlSequenceBuilder Space()
    {
        return Print([SP]);
    }

    public ControlSequenceBuilder SetOutputBatching(bool enable)
    {
        return Print(CSI).Print("?2026").Print(enable ? "h" : "l");
    }

    public ControlSequenceBuilder SetTitle(scoped ReadOnlySpan<char> title)
    {
        return Print(OSC).Print("2;").Print(title).Print(ST);
    }

    public ControlSequenceBuilder PushTitle()
    {
        return Print(CSI).Print("22;2t");
    }

    public ControlSequenceBuilder PopTitle()
    {
        return Print(CSI).Print("23;2t");
    }

    public ControlSequenceBuilder SetProgress(ProgressState state, int value)
    {
        Check.Enum(state);
        Check.Range(Math.Clamp(value, 0, 100) == value, value);

        var stateSpan = (stackalloc char[StackBufferSize]);
        var valueSpan = (stackalloc char[StackBufferSize]);

        _ = ((int)state).TryFormat(stateSpan, out var stateLen, provider: _culture);
        _ = value.TryFormat(valueSpan, out var valueLen, provider: _culture);

        return Print(OSC).Print("9;4;").Print(stateSpan[..stateLen]).Print(";").Print(valueSpan[..valueLen]).Print(ST);
    }

    public ControlSequenceBuilder SetCursorKeyMode(CursorKeyMode mode)
    {
        Check.Enum(mode);

        var ch = (char)mode;

        return Print(CSI).Print("?1").Print([ch]);
    }

    public ControlSequenceBuilder SetKeypadMode(KeypadMode mode)
    {
        Check.Enum(mode);

        var ch = (char)mode;

        return Print([ESC]).Print([ch]);
    }

    public ControlSequenceBuilder SetKeyboardLevel(KeyboardLevel level)
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

    public ControlSequenceBuilder SetAutoRepeatMode(bool enable)
    {
        return Print(CSI).Print("?8").Print(enable ? "h" : "l");
    }

    public ControlSequenceBuilder SetMouseEvents(MouseEvents events)
    {
        return Print(CSI).Print("?1003").Print(events.HasFlag(MouseEvents.Movement) ? "h" : "l")
            .Print(CSI).Print("?1006").Print(events.HasFlag(MouseEvents.Buttons) ? "h" : "l");
    }

    public ControlSequenceBuilder SetMousePointerStyle(scoped ReadOnlySpan<char> style)
    {
        return Print(OSC).Print("22;").Print(style).Print(ST);
    }

    public ControlSequenceBuilder SetFocusEvents(bool enable)
    {
        return Print(CSI).Print("?1004").Print(enable ? "h" : "l");
    }

    public ControlSequenceBuilder SetBracketedPaste(bool enable)
    {
        return Print(CSI).Print("?2004").Print(enable ? "h" : "l");
    }

    public ControlSequenceBuilder SetScreenBuffer(ScreenBuffer buffer)
    {
        Check.Enum(buffer);

        var ch = (char)buffer;

        return Print(CSI).Print("?1049").Print([ch]);
    }

    public ControlSequenceBuilder SetInvertedColors(bool enable)
    {
        return Print(CSI).Print("?5").Print(enable ? "h" : "l");
    }

    public ControlSequenceBuilder SetCursorVisibility(bool visible)
    {
        return Print(CSI).Print("?25").Print(visible ? "h" : "l");
    }

    public ControlSequenceBuilder SetCursorStyle(CursorStyle style)
    {
        Check.Enum(style);

        var styleSpan = (stackalloc char[StackBufferSize]);

        _ = ((int)style).TryFormat(styleSpan, out var styleLen, provider: _culture);

        return Print(CSI).Print(styleSpan[..styleLen]).Space().Print("q");
    }

    public ControlSequenceBuilder SetScrollBarVisibility(bool visible)
    {
        return Print(CSI).Print("?30").Print(visible ? "h" : "l");
    }

    public ControlSequenceBuilder SetScrollMargin(int top, int bottom)
    {
        Check.Range(top >= 0, top);
        Check.Range(bottom > top, bottom);

        var topSpan = (stackalloc char[StackBufferSize]);
        var bottomSpan = (stackalloc char[StackBufferSize]);

        _ = (top + 1).TryFormat(topSpan, out var topLen, provider: _culture);
        _ = (bottom + 1).TryFormat(bottomSpan, out var bottomLen, provider: _culture);

        return Print(CSI).Print(topSpan[..topLen]).Print(";").Print(bottomSpan[..bottomLen]).Print("r");
    }

    public ControlSequenceBuilder ResetScrollMargin()
    {
        return Print(CSI).Print(";r");
    }

    private ControlSequenceBuilder ModifyText(string type, int count)
    {
        Check.Range(count >= 0, count);

        if (count == 0)
            return this;

        var countSpan = (stackalloc char[StackBufferSize]);

        _ = count.TryFormat(countSpan, out var countLen, provider: _culture);

        return Print(CSI).Print(countSpan[..countLen]).Print(type);
    }

    public ControlSequenceBuilder InsertCharacters(int count)
    {
        return ModifyText("@", count);
    }

    public ControlSequenceBuilder DeleteCharacters(int count)
    {
        return ModifyText("P", count);
    }

    public ControlSequenceBuilder EraseCharacters(int count)
    {
        return ModifyText("X", count);
    }

    public ControlSequenceBuilder InsertLines(int count)
    {
        return ModifyText("L", count);
    }

    public ControlSequenceBuilder DeleteLines(int count)
    {
        return ModifyText("M", count);
    }

    private ControlSequenceBuilder Clear(string type, ClearMode mode)
    {
        Check.Enum(mode);

        var modeSpan = (stackalloc char[StackBufferSize]);

        _ = ((int)mode).TryFormat(modeSpan, out var modeLen, provider: _culture);

        return Print(CSI).Print(modeSpan[..modeLen]).Print(type);
    }

    public ControlSequenceBuilder ClearScreen(ClearMode mode = ClearMode.Full)
    {
        return Clear("J", mode);
    }

    public ControlSequenceBuilder ClearLine(ClearMode mode = ClearMode.Full)
    {
        return Clear("K", mode);
    }

    public ControlSequenceBuilder SetProtection(bool protect)
    {
        return Print(CSI).Print(protect ? "1" : "0").Print("\"q");
    }

    private ControlSequenceBuilder ProtectedClear(string type, ClearMode mode)
    {
        Check.Enum(mode);

        var modeSpan = (stackalloc char[StackBufferSize]);

        _ = ((int)mode).TryFormat(modeSpan, out var modeLen, provider: _culture);

        return Print(CSI).Print("?").Print(modeSpan[..modeLen]).Print(type);
    }

    public ControlSequenceBuilder ProtectedClearScreen(ClearMode mode = ClearMode.Full)
    {
        return Clear("J", mode);
    }

    public ControlSequenceBuilder ProtectedClearLine(ClearMode mode = ClearMode.Full)
    {
        return Clear("K", mode);
    }

    private ControlSequenceBuilder MoveBuffer(string type, int count)
    {
        Check.Range(count >= 0, count);

        if (count == 0)
            return this;

        var countSpan = (stackalloc char[StackBufferSize]);

        _ = count.TryFormat(countSpan, out var countLen, provider: _culture);

        return Print(CSI).Print(countSpan[..countLen]).Print(type);
    }

    public ControlSequenceBuilder MoveBufferUp(int count)
    {
        return MoveBuffer("S", count);
    }

    public ControlSequenceBuilder MoveBufferDown(int count)
    {
        return MoveBuffer("T", count);
    }

    public ControlSequenceBuilder MoveCursorTo(int line, int column)
    {
        Check.Range(line >= 0, line);
        Check.Range(column >= 0, column);

        var lineSpan = (stackalloc char[StackBufferSize]);
        var columnSpan = (stackalloc char[StackBufferSize]);

        _ = (line + 1).TryFormat(lineSpan, out var lineLen, provider: _culture);
        _ = (column + 1).TryFormat(columnSpan, out var columnLen, provider: _culture);

        return Print(CSI).Print(lineSpan[..lineLen]).Print(";").Print(columnSpan[..columnLen]).Print("H");
    }

    private ControlSequenceBuilder MoveCursor(string type, int count)
    {
        Check.Range(count >= 0, count);

        if (count == 0)
            return this;

        var countSpan = (stackalloc char[StackBufferSize]);

        _ = count.TryFormat(countSpan, out var countLen, provider: _culture);

        return Print(CSI).Print(countSpan[..countLen]).Print(type);
    }

    public ControlSequenceBuilder MoveCursorUp(int count)
    {
        return MoveCursor("A", count);
    }

    public ControlSequenceBuilder MoveCursorDown(int count)
    {
        return MoveCursor("B", count);
    }

    public ControlSequenceBuilder MoveCursorLeft(int count)
    {
        return MoveCursor("D", count);
    }

    public ControlSequenceBuilder MoveCursorRight(int count)
    {
        return MoveCursor("C", count);
    }

    public ControlSequenceBuilder SaveCursorState()
    {
        return Print([ESC]).Print("7");
    }

    public ControlSequenceBuilder RestoreCursorState()
    {
        return Print([ESC]).Print("8");
    }

    public ControlSequenceBuilder SetForegroundColor(Color color)
    {
        Check.Argument(color.A == byte.MaxValue, color);

        var rSpan = (stackalloc char[StackBufferSize]);
        var gSpan = (stackalloc char[StackBufferSize]);
        var bSpan = (stackalloc char[StackBufferSize]);

        _ = color.R.TryFormat(rSpan, out var rLen, provider: _culture);
        _ = color.G.TryFormat(gSpan, out var gLen, provider: _culture);
        _ = color.B.TryFormat(bSpan, out var bLen, provider: _culture);

        return Print(CSI).Print("38;2;").Print(rSpan[..rLen]).Print(";")
            .Print(gSpan[..gLen]).Print(";").Print(bSpan[..bLen]).Print("m");
    }

    public ControlSequenceBuilder SetBackgroundColor(Color color)
    {
        Check.Argument(color.A == byte.MaxValue, color);

        var rSpan = (stackalloc char[StackBufferSize]);
        var gSpan = (stackalloc char[StackBufferSize]);
        var bSpan = (stackalloc char[StackBufferSize]);

        _ = color.R.TryFormat(rSpan, out var rLen, provider: _culture);
        _ = color.G.TryFormat(gSpan, out var gLen, provider: _culture);
        _ = color.B.TryFormat(bSpan, out var bLen, provider: _culture);

        return Print(CSI).Print("48;2;").Print(rSpan[..rLen]).Print(";")
            .Print(gSpan[..gLen]).Print(";").Print(bSpan[..bLen]).Print("m");
    }

    public ControlSequenceBuilder SetUnderlineColor(Color color)
    {
        Check.Argument(color.A == byte.MaxValue, color);

        var rSpan = (stackalloc char[StackBufferSize]);
        var gSpan = (stackalloc char[StackBufferSize]);
        var bSpan = (stackalloc char[StackBufferSize]);

        _ = color.R.TryFormat(rSpan, out var rLen, provider: _culture);
        _ = color.G.TryFormat(gSpan, out var gLen, provider: _culture);
        _ = color.B.TryFormat(bSpan, out var bLen, provider: _culture);

        return Print(CSI).Print("58;2;").Print(rSpan[..rLen]).Print(";")
            .Print(gSpan[..gLen]).Print(";").Print(bSpan[..bLen]).Print("m");
    }

    public ControlSequenceBuilder SetDecorations(
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

    public ControlSequenceBuilder ResetAttributes()
    {
        return Print(CSI).Print("0m");
    }

    public ControlSequenceBuilder OpenHyperlink(Uri uri, scoped ReadOnlySpan<char> id = default)
    {
        Check.Null(uri);

        _ = Print(OSC).Print("8;");

        if (!id.IsEmpty)
            _ = Print("id=").Print(id);

        return Print(";").Print(uri.ToString()).Print(ST);
    }

    public ControlSequenceBuilder CloseHyperlink()
    {
        return Print(OSC).Print("8;;").Print(ST);
    }

    public ControlSequenceBuilder SetWorkingDirectory(Uri uri)
    {
        Check.Null(uri);
        Check.Argument(uri.Scheme == Uri.UriSchemeFile, uri);

        return Print(OSC).Print("7").Print(uri.ToString()).Print(ST);
    }

    public ControlSequenceBuilder SetWorkingDirectory(scoped ReadOnlySpan<char> path)
    {
        Check.Argument(!path.IsEmpty, path);

        return Print(OSC).Print("9;9;").Print("\"").Print(path).Print("\"").Print(ST);
    }

    public ControlSequenceBuilder BeginShellPrompt()
    {
        return Print(OSC).Print("133;A").Print(ST);
    }

    public ControlSequenceBuilder EndShellPrompt()
    {
        return Print(OSC).Print("133;B").Print(ST);
    }

    public ControlSequenceBuilder BeginShellExecution()
    {
        return Print(OSC).Print("133;C").Print(ST);
    }

    public ControlSequenceBuilder EndShellExecution(int? code = null)
    {
        _ = Print(OSC).Print("133;D");

        if (code is { } c)
        {
            var codeSpan = (stackalloc char[StackBufferSize]);

            _ = c.TryFormat(codeSpan, out var codeLen, provider: _culture);

            _ = Print(";").Print(codeSpan[..codeLen]);
        }

        return Print(ST);
    }

    public ControlSequenceBuilder SaveScreenshot(ScreenshotFormat format = ScreenshotFormat.Html)
    {
        Check.Enum(format);

        var formatSpan = (stackalloc char[StackBufferSize]);

        _ = ((int)format).TryFormat(formatSpan, out var formatLen, provider: _culture);

        return Print(CSI).Print(formatSpan[..formatLen]).Print("i");
    }

    public ControlSequenceBuilder PlayNotes(int volume, int duration, scoped ReadOnlySpan<int> notes)
    {
        Check.Range(volume is >= 0 and <= 7, volume);
        Check.Range(duration >= 0, duration);
        Check.Argument(notes.Length >= 1, nameof(notes));
        Check.All(notes, static note => note is >= 1 and <= 25);

        var volumeSpan = (stackalloc char[StackBufferSize]);
        var durationSpan = (stackalloc char[StackBufferSize]);

        _ = volume.TryFormat(volumeSpan, out var volumeLen, provider: _culture);
        _ = duration.TryFormat(durationSpan, out var durationLen, provider: _culture);

        _ = Print(CSI).Print(volumeSpan[..volumeLen]).Print(";").Print(durationSpan[..durationLen]);

        var noteSpan = (stackalloc char[StackBufferSize]);

        foreach (var note in notes)
        {
            _ = note.TryFormat(noteSpan, out var noteLen, provider: _culture);

            _ = Print(";").Print(noteSpan[..noteLen]);
        }

        return Print(",~");
    }

    public ControlSequenceBuilder SoftReset()
    {
        return Print(CSI).Print("!p");
    }

    public ControlSequenceBuilder FullReset()
    {
        return Print([ESC]).Print("c");
    }

    public override string ToString()
    {
        return Span.ToString();
    }
}
