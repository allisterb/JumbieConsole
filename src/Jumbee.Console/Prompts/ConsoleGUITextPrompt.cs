namespace Jumbee.Console.Prompts;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

using ConsoleGUI.Api;
using ConsoleGUI.Common;
using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.Space;
using ConsoleGUI.Utils;

using Spectre.Console;
using ConsoleGuiSize = ConsoleGUI.Space.Size;
using Jumbee.Console;

public class ConsoleGUITextPrompt<T> : Control, IInputListener, IDisposable where T : IConvertible
{
    #region Constructors
    public ConsoleGUITextPrompt(string prompt, StringComparer? comparer = null, bool enableCursorBlink = false)
    {
        _prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
        _comparer = comparer;
        _bufferConsole = new BufferConsole();
        _ansiConsole = new AnsiConsoleBuffer(_bufferConsole);

        if (enableCursorBlink)
        {
            UIUpdate.Tick += OnCursorBlink;
        }
    }
    #endregion

    #region Properties
    public Style? PromptStyle { get; set; }
    public List<T> Choices { get; } = new List<T>();
    public CultureInfo? Culture { get; set; }
    public string InvalidChoiceMessage { get; set; } = "[red]Please select one of the available options[/]";
    public bool IsSecret { get; set; }
    public char? Mask { get; set; } = '*';
    public string ValidationErrorMessage { get; set; } = "[red]Invalid input[/]";
    public bool ShowChoices { get; set; } = true;
    public bool ShowDefaultValue { get; set; } = true;
    public bool AllowEmpty { get; set; }
    public Func<T, ValidationResult>? Validator { get; set; }
    public Style? DefaultValueStyle { get; set; }
    public Style? ChoicesStyle { get; set; }
    public bool ShowCursor { get; set; } = true;

    internal DefaultPromptValue<T>? DefaultValue { get; set; }
    #endregion

    #region Methods
    public void SetDefaultValue(T value)
    {
        DefaultValue = new DefaultPromptValue<T>(value);
    }

    private void OnCursorBlink(object? sender, UIUpdateTimerEventArgs e)
    {
        lock (e.Lock)
        {
            _blinkState = !_blinkState;
            if (ShowCursor)
            {
                Redraw();
            }
        }
    }

    public void Dispose()
    {
        UIUpdate.Tick -= OnCursorBlink;
    }

    public override Cell this[Position position]
    {
        get
        {
            lock (UIUpdate.Lock)
            {
                Cell cell = _emptyCell;
                if (_bufferConsole.Buffer != null &&
                    position.X >= 0 && position.X < Size.Width &&
                    position.Y >= 0 && position.Y < Size.Height)
                {
                    cell = _bufferConsole.Buffer[position.X, position.Y];
                }

                // Render Cursor
                if (ShowCursor && _blinkState &&
                    position.X == _inputStartX + _caretPosition &&
                    position.Y == _inputStartY)
                {
                    if (cell.Content == null || cell.Content == '\0')
                    {
                        return _cursorEmptyCell;
                    }
                    return cell.WithBackground(_cursorBackgroundColor);
                }

                return cell;
            }
        }
    }

    protected override void Initialize()
    {
        lock (UIUpdate.Lock)
        {
            var targetSize = MaxSize;
            if (targetSize.Width > 1000) targetSize = new ConsoleGuiSize(1000, targetSize.Height);
            if (targetSize.Height > 1000) targetSize = new ConsoleGuiSize(targetSize.Width, 1000);

            Resize(targetSize);
            _bufferConsole.Resize(Size);

            Render();
        }
    }

    private void Render()
    {
        // Assumes lock is held by caller (Initialize or OnInput)
        if (Size.Width <= 0 || Size.Height <= 0) return;

        _ansiConsole.Clear(true);

        // 1. Build Prompt Markup
        var builder = new StringBuilder();
        builder.Append(_prompt.TrimEnd());

        var appendSuffix = false;
   
        if (ShowChoices && Choices.Count > 0)
        {
            appendSuffix = true;
            var choices = string.Join("/", Choices.Select(choice => choice.ToString()));
            var choicesStyle = ChoicesStyle?.ToMarkup() ?? "blue";
            builder.AppendFormat(CultureInfo.InvariantCulture, " [{0}][[{1}]][/]", choicesStyle, choices);
        }

        if (ShowDefaultValue && DefaultValue != null)
        {
            appendSuffix = true;
            var defaultValueStyle = DefaultValueStyle?.ToMarkup() ?? "green";
            var defaultValue = DefaultValue.Value.Value.ToString() ?? "";
            var displayDefault = IsSecret && Mask.HasValue ? new string(Mask.Value, defaultValue.Length) : defaultValue;

            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                " [{0}]({1})[/]",
                defaultValueStyle,
                displayDefault);
        }

        var markup = builder.ToString().Trim();
        if (appendSuffix)
        {
            markup += ":";
        }

        _ansiConsole.Markup(markup + " ");

        _inputStartX = _ansiConsole.CursorX;
        _inputStartY = _ansiConsole.CursorY;

        // 2. Render Input
        string displayInput = _input;
        if (IsSecret && Mask.HasValue)
        {
            displayInput = new string(Mask.Value, _input.Length);
        }

        _ansiConsole.Write(displayInput);

        // 3. Render Error (if any)
        if (_validationError != null)
        {
            _ansiConsole.WriteLine();
            _ansiConsole.MarkupLine(_validationError);
        }

        Redraw();
    }

    void IInputListener.OnInput(InputEvent inputEvent)
    {
        lock (UIUpdate.Lock)
        {
            bool handled = false;
            string? newInput = null;

            _blinkState = true;

            switch (inputEvent.Key.Key)
            {
                case ConsoleKey.LeftArrow:
                    _caretPosition = Math.Max(0, _caretPosition - 1);
                    handled = true;
                    break;
                case ConsoleKey.RightArrow:
                    _caretPosition = Math.Min(_input.Length, _caretPosition + 1);
                    handled = true;
                    break;
                case ConsoleKey.Home:
                    _caretPosition = 0;
                    handled = true;
                    break;
                case ConsoleKey.End:
                    _caretPosition = _input.Length;
                    handled = true;
                    break;
                case ConsoleKey.Backspace:
                    if (_caretPosition > 0)
                    {
                        newInput = _input.Remove(_caretPosition - 1, 1);
                        _caretPosition--;
                        handled = true;
                    }
                    break;
                case ConsoleKey.Delete:
                    if (_caretPosition < _input.Length)
                    {
                        newInput = _input.Remove(_caretPosition, 1);
                        handled = true;
                    }
                    break;
                case ConsoleKey.Enter:
                    AttemptCommit();
                    handled = true;
                    break;
                default:
                    if (!char.IsControl(inputEvent.Key.KeyChar))
                    {
                        newInput = _input.Insert(_caretPosition, inputEvent.Key.KeyChar.ToString());
                        _caretPosition++;
                        handled = true;
                    }
                    break;
            }

            if (newInput != null)
            {
                _input = newInput;
            }

            if (handled)
            {
                inputEvent.Handled = true;
                Render();
            }
        }
    }

    private void AttemptCommit()
    {
        var result = (T) Convert.ChangeType(_input, typeof(T));
        if (string.IsNullOrWhiteSpace(_input))
        {
            if (DefaultValue != null)
            {
                Committed?.Invoke(this, DefaultValue.Value.Value);
                return;
            }

            if (AllowEmpty)
            {                
                Committed?.Invoke(this, default);
                return;
                
            }
            return;
        }

        var choiceMap = Choices.ToDictionary(choice => choice.ToString()!, choice => choice, _comparer);
        if (Choices.Count > 0)
        {
            if (choiceMap.TryGetValue(_input, out result) && result != null)
            {
                // Valid choice
            }
            else
            {
                _validationError = InvalidChoiceMessage;
                Render();
                return;
            }
        }
        else if (result == null)
        {                        
                _validationError = ValidationErrorMessage;
                Render();
                return;
            
        }

        if (Validator != null)
        {
            var validationResult = Validator(result);
            if (!validationResult.Successful)
            {
                _validationError = validationResult.Message ?? ValidationErrorMessage;
                Render();
                return;
            }
        }

        _validationError = null;
        Committed?.Invoke(this, result);
    }
    #endregion

    #region Fields
    private readonly string _prompt;
    private readonly StringComparer? _comparer;
    private readonly BufferConsole _bufferConsole;
    private readonly AnsiConsoleBuffer _ansiConsole;

    private string _input = string.Empty;
    private int _caretPosition = 0;
    private string? _validationError = null;
    private int _inputStartX = 0;
    private int _inputStartY = 0;

    private bool _blinkState = true;
    private static readonly Cell _emptyCell = new Cell(Character.Empty);
    private static readonly ConsoleGUI.Data.Color _cursorBackgroundColor = new ConsoleGUI.Data.Color(100, 100, 100);
    private static readonly Cell _cursorEmptyCell = new Cell(' ').WithBackground(_cursorBackgroundColor);
    #endregion

    #region Events
    public event EventHandler<T?>? Committed;
    #endregion
}

internal struct DefaultPromptValue<T>
{
    public T Value { get; }
    public DefaultPromptValue(T value) => Value = value;
}

