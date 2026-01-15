// SPDX-License-Identifier: 0BSD

using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Text;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="AnsiControlSequenceBuilder"/> class.
    /// </summary>
    /// <param name="capacity">The initial capacity. Must be greater than 0.</param>
    public AnsiControlSequenceBuilder(int capacity = 1024)
    {
        _capacity = capacity;
        _writer = new(capacity);
    }

    /// <summary>
    /// Clears the builder, optionally reallocating the underlying buffer.
    /// </summary>
    /// <param name="reallocateThreshold">The reallocation threshold. Must be greater than or equal to 0.</param>
    public void Clear(int reallocateThreshold = 4096)
    {
        if (reallocateThreshold != 0 && _writer.Capacity > reallocateThreshold)
            _writer = new(_capacity);
        else
            _writer.Clear();
    }

    /// <summary>
    /// Appends a span of characters to the builder.
    /// </summary>
    /// <param name="value">The characters to append.</param>
    /// <returns>A reference to this instance after the append operation has completed.</returns>
    public AnsiControlSequenceBuilder Print(scoped ReadOnlySpan<char> value)
    {
        _writer.Write(value);

        return this;
    }

    /// <summary>
    /// Appends a span of characters to the builder.
    /// </summary>
    /// <param name="value">The characters to append.</param>
    /// <returns>A reference to this instance after the append operation has completed.</returns>
    public AnsiControlSequenceBuilder PrintChar(char value)
    {
        ReadOnlySpan<char> charSpan = stackalloc[] { value };
        _writer.Write(charSpan);

        return this;
    }
    /// <summary>
    /// Appends the string returned by an interpolated string handler to the builder.
    /// </summary>
    /// <param name="handler">The interpolated string handler.</param>
    /// <returns>A reference to this instance after the append operation has completed.</returns>
    [SuppressMessage("", "IDE0060")]
    public AnsiControlSequenceBuilder Print(
        [InterpolatedStringHandlerArgument("")] scoped ref PrintInterpolatedStringHandler handler) => this;

    /// <summary>
    /// Appends the string returned by an interpolated string handler to the builder, using the specified format provider.
    /// </summary>
    /// <param name="provider">An object that supplies culture-specific formatting information.</param>
    /// <param name="handler">The interpolated string handler.</param>
    /// <returns>A reference to this instance after the append operation has completed.</returns>
    [SuppressMessage("", "IDE0060")]
    public AnsiControlSequenceBuilder Print(
        IFormatProvider? provider,
        [InterpolatedStringHandlerArgument("", nameof(provider))] scoped ref PrintInterpolatedStringHandler handler) => this;

    /// <summary>
    /// Appends a new line to the builder.
    /// </summary>
    /// <returns>A reference to this instance after the append operation has completed.</returns>
    public AnsiControlSequenceBuilder PrintLine() => Print(Environment.NewLine);

    /// <summary>
    /// Appends a span of characters followed by a new line to the builder.
    /// </summary>
    /// <param name="value">The characters to append.</param>
    /// <returns>A reference to this instance after the append operation has completed.</returns>
    public AnsiControlSequenceBuilder PrintLine(scoped ReadOnlySpan<char> value) => Print(value).PrintLine();

    /// <summary>
    /// Appends the string returned by an interpolated string handler followed by a new line to the builder.
    /// </summary>
    /// <param name="handler">The interpolated string handler.</param>
    /// <returns>A reference to this instance after the append operation has completed.</returns>
    [SuppressMessage("", "IDE0060")]
    public AnsiControlSequenceBuilder PrintLine(
        [InterpolatedStringHandlerArgument("")] scoped ref PrintInterpolatedStringHandler handler) => PrintLine();

    /// <summary>
    /// Appends the string returned by an interpolated string handler followed by a new line to the builder, using the specified format provider.
    /// </summary>
    /// <param name="provider">An object that supplies culture-specific formatting information.</param>
    /// <param name="handler">The interpolated string handler.</param>
    /// <returns>A reference to this instance after the append operation has completed.</returns>
    [SuppressMessage("", "IDE0060")]
    public AnsiControlSequenceBuilder PrintLine(
        IFormatProvider? provider,
        [InterpolatedStringHandlerArgument("", nameof(provider))] scoped ref PrintInterpolatedStringHandler handler) => PrintLine();

    // Keep methods in sync with the ControlSequences class.

    /// <summary>
    /// Appends a Null (NUL) character.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder Null() => Print([NUL]);

    /// <summary>
    /// Appends a Bell (BEL) character, which may cause an audible beep.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder Beep() => Print([BEL]);

    /// <summary>
    /// Appends a Backspace (BS) character.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder Backspace() => Print([BS]);

    /// <summary>
    /// Appends a Horizontal Tab (HT) character.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder HorizontalTab() => Print([HT]);

    /// <summary>
    /// Appends a Line Feed (LF) character.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder LineFeed() => Print([LF]);

    /// <summary>
    /// Appends a Vertical Tab (VT) character.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder VerticalTab() => Print([VT]);

    /// <summary>
    /// Appends a Form Feed (FF) character.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder FormFeed() => Print([FF]);

    /// <summary>
    /// Appends a Carriage Return (CR) character.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder CarriageReturn() => Print([CR]);

    /// <summary>
    /// Appends a Substitute (SUB) character.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder Substitute() => Print([SUB]);

    /// <summary>
    /// Appends a Cancel (CAN) character.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder Cancel() => Print([CAN]);

    /// <summary>
    /// Appends a File Separator (FS) character.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder FileSeparator() => Print([FS]);

    /// <summary>
    /// Appends a Group Separator (GS) character.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder GroupSeparator() => Print([GS]);

    /// <summary>
    /// Appends a Record Separator (RS) character.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder RecordSeparator() => Print([RS]);

    /// <summary>
    /// Appends a Unit Separator (US) character.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder UnitSeparator() => Print([US]);

    /// <summary>
    /// Appends a Space (SP) character.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder Space() => Print([SP]);

    /// <summary>
    /// Enables or disables terminal output batching.
    /// </summary>
    /// <param name="enable">True to enable, false to disable.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder SetOutputBatching(bool enable) => Print(CSI).Print("?2026").Print(enable ? "h" : "l");

    /// <summary>
    /// Sets the terminal window title.
    /// </summary>
    /// <param name="title">The title to set.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder SetTitle(scoped ReadOnlySpan<char> title) => Print(OSC).Print("2;").Print(title).Print(ST);

    /// <summary>
    /// Pushes the current window title onto the title stack.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder PushTitle() => Print(CSI).Print("22;2t");

    /// <summary>
    /// Pops the window title from the title stack.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder PopTitle() => Print(CSI).Print("23;2t");

    /// <summary>
    /// Sets the progress indicator in the title bar.
    /// </summary>
    /// <param name="state">The progress state. Must be a defined enum value.</param>
    /// <param name="value">The progress value. Must be between 0 and 100.</param>
    public AnsiControlSequenceBuilder SetProgress(ProgressState state, int value) => Print(_culture, $"{OSC}9;4;{(int)state};{value}{ST}");

    /// <summary>
    /// Sets the cursor key mode.
    /// </summary>
    /// <param name="mode">The cursor key mode. Must be a defined enum value.</param>
    public AnsiControlSequenceBuilder SetCursorKeyMode(CursorKeyMode mode) => Print(CSI).Print("?1").Print([(char)mode]);

    /// <summary>
    /// Sets the keypad mode.
    /// </summary>
    /// <param name="mode">The keypad mode. Must be a defined enum value.</param>
    public AnsiControlSequenceBuilder SetKeypadMode(KeypadMode mode) => Print([ESC]).Print([(char)mode]);

    /// <summary>
    /// Sets the keyboard conformance level.
    /// </summary>
    /// <param name="level">The keyboard level to set.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
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

    /// <summary>
    /// Enables or disables key auto-repeat mode.
    /// </summary>
    /// <param name="enable">True to enable, false to disable.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder SetAutoRepeatMode(bool enable) => Print(CSI).Print("?8").Print(enable ? "h" : "l");

    /// <summary>
    /// Enables or disables mouse event reporting.
    /// </summary>
    /// <param name="events">The types of mouse events to report.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder SetMouseEvents(MouseEvents events) => Print(CSI).Print("?1003").Print(events.HasFlag(MouseEvents.Movement) ? "h" : "l")
            .Print(CSI).Print("?1006").Print(events.HasFlag(MouseEvents.Buttons) ? "h" : "l");

    /// <summary>
    /// Sets the shape of the mouse pointer.
    /// </summary>
    /// <param name="style">The style of the mouse pointer.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder SetMousePointerStyle(scoped ReadOnlySpan<char> style) => Print(OSC).Print("22;").Print(style).Print(ST);

    /// <summary>
    /// Enables or disables focus event reporting.
    /// </summary>
    /// <param name="enable">True to enable, false to disable.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder SetFocusEvents(bool enable) => Print(CSI).Print("?1004").Print(enable ? "h" : "l");

    /// <summary>
    /// Enables or disables bracketed paste mode.
    /// </summary>
    /// <param name="enable">True to enable, false to disable.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder SetBracketedPaste(bool enable) => Print(CSI).Print("?2004").Print(enable ? "h" : "l");

    /// <summary>
    /// Sets the active screen buffer.
    /// </summary>
    /// <param name="buffer">The screen buffer. Must be a defined enum value.</param>
    public AnsiControlSequenceBuilder SetScreenBuffer(ScreenBuffer buffer) => Print(CSI).Print("?1049").Print([(char)buffer]);

    /// <summary>
    /// Enables or disables inverted screen colors (light/dark mode).
    /// </summary>
    /// <param name="enable">True to enable, false to disable.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder SetInvertedColors(bool enable) => Print(CSI).Print("?5").Print(enable ? "h" : "l");

    /// <summary>
    /// Sets the cursor visibility.
    /// </summary>
    /// <param name="visible">True to show the cursor, false to hide it.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder SetCursorVisibility(bool visible) => Print(CSI).Print("?25").Print(visible ? "h" : "l");

    /// <summary>
    /// Sets the cursor style.
    /// </summary>
    /// <param name="style">The cursor style. Must be a defined enum value.</param>
    public AnsiControlSequenceBuilder SetCursorStyle(CursorStyle style) => Print(_culture, $"{CSI}{(int)style} q");

    /// <summary>
    /// Sets the scroll bar visibility.
    /// </summary>
    /// <param name="visible">True to show the scroll bar, false to hide it.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder SetScrollBarVisibility(bool visible) => Print(CSI).Print("?30").Print(visible ? "h" : "l");

    /// <summary>
    /// Sets the scroll margin.
    /// </summary>
    /// <param name="top">The top margin. Must be greater than or equal to 0.</param>
    /// <param name="bottom">The bottom margin. Must be greater than top.</param>
    public AnsiControlSequenceBuilder SetScrollMargin(int top, int bottom) => Print(_culture, $"{CSI}{top + 1};{bottom + 1}r");

    /// <summary>
    /// Resets the scroll margin to the full window.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder ResetScrollMargin() => Print(CSI).Print(";r");

    /// <summary>
    /// Modifies text in the buffer.
    /// </summary>
    /// <param name="type">The modification type.</param>
    /// <param name="count">The number of characters/lines. Must be greater than or equal to 0.</param>
    private AnsiControlSequenceBuilder ModifyText(string type, int count)
    {
        if (count == 0)
            return this;

        return Print(_culture, $"{CSI}{count}{type}");
    }

    /// <summary>
    /// Inserts a specified number of characters.
    /// </summary>
    /// <param name="count">The number of characters to insert.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder InsertCharacters(int count) => ModifyText("@", count);

    /// <summary>
    /// Deletes a specified number of characters.
    /// </summary>
    /// <param name="count">The number of characters to delete.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder DeleteCharacters(int count) => ModifyText("P", count);

    /// <summary>
    /// Erases a specified number of characters.
    /// </summary>
    /// <param name="count">The number of characters to erase.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder EraseCharacters(int count) => ModifyText("X", count);

    /// <summary>
    /// Inserts a specified number of lines.
    /// </summary>
    /// <param name="count">The number of lines to insert.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder InsertLines(int count) => ModifyText("L", count);

    /// <summary>
    /// Deletes a specified number of lines.
    /// </summary>
    /// <param name="count">The number of lines to delete.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder DeleteLines(int count) => ModifyText("M", count);

    /// <summary>
    /// Clears a part of the screen or line.
    /// </summary>
    /// <param name="type">The clear type.</param>
    /// <param name="mode">The clear mode. Must be a defined enum value.</param>
    private AnsiControlSequenceBuilder Clear(string type, ClearMode mode) => Print(_culture, $"{CSI}{(int)mode}{type}");

    /// <summary>
    /// Clears the screen.
    /// </summary>
    /// <param name="mode">The clear mode to use.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder ClearScreen(ClearMode mode = ClearMode.Full) => Clear("J", mode);

    /// <summary>
    /// Clears the current line.
    /// </summary>
    /// <param name="mode">The clear mode to use.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder ClearLine(ClearMode mode = ClearMode.Full) => Clear("K", mode);

    /// <summary>
    /// Sets the character protection attribute.
    /// </summary>
    /// <param name="protect">True to protect characters, false to unprotect.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder SetProtection(bool protect) => Print(CSI).Print(protect ? "1" : "0").Print("\"q");

    /// <summary>
    /// Clears a part of the screen or line, respecting protected areas.
    /// </summary>
    /// <param name="type">The clear type.</param>
    /// <param name="mode">The clear mode. Must be a defined enum value.</param>
    private AnsiControlSequenceBuilder ProtectedClear(string type, ClearMode mode) => Print(_culture, $"{CSI}?{(int)mode}{type}");

    /// <summary>
    /// Clears the screen, respecting protected areas.
    /// </summary>
    /// <param name="mode">The clear mode to use.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder ProtectedClearScreen(ClearMode mode = ClearMode.Full) => ProtectedClear("J", mode);

    /// <summary>
    /// Clears the current line, respecting protected areas.
    /// </summary>
    /// <param name="mode">The clear mode to use.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder ProtectedClearLine(ClearMode mode = ClearMode.Full) => ProtectedClear("K", mode);

    /// <summary>
    /// Moves the buffer content.
    /// </summary>
    /// <param name="type">The move direction.</param>
    /// <param name="count">The number of lines. Must be greater than or equal to 0.</param>
    private AnsiControlSequenceBuilder MoveBuffer(string type, int count)
    {
        if (count == 0)
            return this;

        return Print(_culture, $"{CSI}{count}{type}");
    }

    /// <summary>
    /// Scrolls the buffer content up.
    /// </summary>
    /// <param name="count">The number of lines to scroll.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder MoveBufferUp(int count) => MoveBuffer("S", count);

    /// <summary>
    /// Scrolls the buffer content down.
    /// </summary>
    /// <param name="count">The number of lines to scroll.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder MoveBufferDown(int count) => MoveBuffer("T", count);

    /// <summary>
    /// Moves the cursor to a specific position.
    /// </summary>
    /// <param name="line">The line. Must be greater than or equal to 0.</param>
    /// <param name="column">The column. Must be greater than or equal to 0.</param>
    public AnsiControlSequenceBuilder MoveCursorTo(int line, int column) => Print(_culture, $"{CSI}{line + 1};{column + 1}H");

    /// <summary>
    /// Moves the cursor relative to its current position.
    /// </summary>
    /// <param name="type">The move direction.</param>
    /// <param name="count">The number of positions. Must be greater than or equal to 0.</param>
    private AnsiControlSequenceBuilder MoveCursor(string type, int count)
    {
        if (count == 0)
            return this;

        return Print(_culture, $"{CSI}{count}{type}");
    }

    /// <summary>
    /// Moves the cursor up a specified number of lines.
    /// </summary>
    /// <param name="count">The number of lines to move.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder MoveCursorUp(int count) => MoveCursor("A", count);

    /// <summary>
    /// Moves the cursor down a specified number of lines.
    /// </summary>
    /// <param name="count">The number of lines to move.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder MoveCursorDown(int count) => MoveCursor("B", count);

    /// <summary>
    /// Moves the cursor left a specified number of columns.
    /// </summary>
    /// <param name="count">The number of columns to move.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder MoveCursorLeft(int count) => MoveCursor("D", count);

    /// <summary>
    /// Moves the cursor right a specified number of columns.
    /// </summary>
    /// <param name="count">The number of columns to move.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder MoveCursorRight(int count) => MoveCursor("C", count);

    /// <summary>
    /// Saves the current cursor state (position, attributes).
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder SaveCursorState() => Print([ESC]).Print("7");

    /// <summary>
    /// Restores the previously saved cursor state.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder RestoreCursorState() => Print([ESC]).Print("8");

    /// <summary>
    /// Sets the foreground color.
    /// </summary>
    /// <param name="color">The color. The alpha component must be 255.</param>
    public AnsiControlSequenceBuilder SetForegroundColor(Color color) => Print(_culture, $"{CSI}38;2;{color.R};{color.G};{color.B}m");

    /// <summary>
    /// Sets the foreground color.
    /// </summary>
    /// <param name="color">The color. The alpha component must be 255.</param>
    public AnsiControlSequenceBuilder SetForegroundColor(int R, int G, int B) => Print(_culture, $"{CSI}38;2;{R};{G};{B}m");
    
    /// <summary>
    /// Sets the background color.
    /// </summary>
    /// <param name="color">The color. The alpha component must be 255.</param>
    public AnsiControlSequenceBuilder SetBackgroundColor(Color color) => Print(_culture, $"{CSI}48;2;{color.R};{color.G};{color.B}m");

    /// <summary>
    /// Sets the background color.
    /// </summary>
    /// <param name="color">The color. The alpha component must be 255.</param>
    public AnsiControlSequenceBuilder SetBackgroundColor(int R, int G, int B) => Print(_culture, $"{CSI}48;2;{R};{G};{B}m");

    /// <summary>
    /// Sets the underline color.
    /// </summary>
    /// <param name="color">The color. The alpha component must be 255.</param>
    public AnsiControlSequenceBuilder SetUnderlineColor(Color color) => Print(_culture, $"{CSI}58;2;{color.R};{color.G};{color.B}m");

    /// <summary>
    /// Sets various text decorations.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
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

    /// <summary>
    /// Resets all graphic attributes to their default state.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder ResetAttributes() => Print(CSI).Print("0m");

    /// <summary>
    /// Opens a hyperlink.
    /// </summary>
    /// <param name="uri">The hyperlink URI. Cannot be null.</param>
    /// <param name="id">The hyperlink ID.</param>
    public AnsiControlSequenceBuilder OpenHyperlink(Uri uri, scoped ReadOnlySpan<char> id = default)
    {
        _ = Print(OSC).Print("8;");

        if (!id.IsEmpty)
            _ = Print("id=").Print(id);

        return Print(";").Print(uri.ToString()).Print(ST);
    }

    /// <summary>
    /// Closes a hyperlink.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder CloseHyperlink() => Print(OSC).Print("8;;").Print(ST);

    /// <summary>
    /// Sets the working directory.
    /// </summary>
    /// <param name="uri">The working directory URI. Cannot be null and must be a file URI.</param>
    public AnsiControlSequenceBuilder SetWorkingDirectory(Uri uri) => Print(OSC).Print("7").Print(uri.ToString()).Print(ST);

    /// <summary>
    /// Sets the working directory.
    /// </summary>
    /// <param name="path">The working directory path. Cannot be empty.</param>
    public AnsiControlSequenceBuilder SetWorkingDirectory(scoped ReadOnlySpan<char> path) => Print(OSC).Print("9;9;").Print("\"").Print(path).Print("\"").Print(ST);

    /// <summary>
    /// Marks the beginning of a shell prompt.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder BeginShellPrompt() => Print(OSC).Print("133;A").Print(ST);

    /// <summary>
    /// Marks the end of a shell prompt.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder EndShellPrompt() => Print(OSC).Print("133;B").Print(ST);

    /// <summary>
    /// Marks the beginning of a shell command execution.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder BeginShellExecution() => Print(OSC).Print("133;C").Print(ST);

    /// <summary>
    /// Marks the end of a shell command execution.
    /// </summary>
    /// <param name="code">The exit code of the command.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder EndShellExecution(int? code = null)
    {
        _ = Print(OSC).Print("133;D");

        if (code is { } c)
        {
            _ = Print(_culture, $";{c}");
        }

        return Print(ST);
    }

    /// <summary>
    /// Saves a screenshot of the terminal.
    /// </summary>
    /// <param name="format">The screenshot format. Must be a defined enum value.</param>
    public AnsiControlSequenceBuilder SaveScreenshot(ScreenshotFormat format = ScreenshotFormat.Html) => Print(_culture, $"{CSI}{(int)format}i");

    /// <summary>
    /// Plays a sequence of musical notes.
    /// </summary>
    /// <param name="volume">The note volume. Must be between 0 and 7.</param>
    /// <param name="duration">The note duration. Must be greater than or equal to 0.</param>
    /// <param name="notes">The notes. Must have at least one note and all notes must be between 1 and 25.</param>
    public AnsiControlSequenceBuilder PlayNotes(int volume, int duration, scoped ReadOnlySpan<int> notes)
    {
        Print(_culture, $"{CSI}{volume};{duration}");

        foreach (var note in notes)
        {
            Print(_culture, $";{note}");
        }

        return Print(",~");
    }

    /// <summary>
    /// Performs a soft terminal reset.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder SoftReset() => Print(CSI).Print("!p");

    /// <summary>
    /// Performs a full terminal reset.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public AnsiControlSequenceBuilder FullReset() => Print([ESC]).Print("c");

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => Span.ToString();

    /// <summary>
    /// Writes the content of the builder to the system console.
    /// </summary>
    public void WriteToSystemConsole() => Console.Write(Span);

    /// <summary>
    /// Asynchronously writes the content of the builder to the system console.
    /// </summary>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    public async Task WriteToSystemConsoleAsync()
    {
        var maxBytes = Encoding.UTF8.GetMaxByteCount(Span.Length);
        byte[] buf = ArrayPool<byte>.Shared.Rent(maxBytes);
        var c = Encoding.UTF8.GetBytes(Span, buf);
        using var s = Console.OpenStandardOutput();
        await s.WriteAsync(buf);
        await s.FlushAsync();
    }
}