namespace Jumbee.Console;

using System;
using System.Globalization;

using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.Space;
using Spectre.Console;

public class TextPrompt : Prompt
{
    #region Constructors
    public TextPrompt(string prompt, StringComparer? comparer = null, bool showCursor = true, bool blinkCursor = false)
    {
        this._prompt = prompt;
        this._comparer = comparer;
        this._showCursor = showCursor;
        this._blinkCursor = blinkCursor;
        this.newInput = "";
    }
    #endregion

    #region Properties
    public Style? PromptStyle { get; set; }
    public CultureInfo? Culture { get; set; }
    public bool IsSecret { get; set; }
    public char? Mask { get; set; } = '*';
    public bool AllowEmpty { get; set; }
    public Func<string, ValidationResult>? Validator { get; set; }
    public string ValidationErrorMessage { get; set; } = "Invalid input.";  
    public Style? DefaultValueStyle { get; set; }
    public bool ShowCursor 
    { 
        get => _showCursor; 
        set
        {
            _showCursor = value;
            if (!_showCursor)
            {
                _blinkState = false;
            }
            Invalidate();
        } 
    
    }
    public bool BlinkCursor 
    { 
        get => _blinkCursor;

        set
        {
            _blinkCursor = value;
            if (!_blinkCursor)
            {
                _blinkState = true;
            }
        }
    }
    #endregion

    #region Methods
    public override Cell this[Position position]
    {
        get
        {
            lock (UI.Lock)
            {
                Cell cell = _emptyCell;
                if (
                    position.X >= 0 && position.X < Size.Width &&
                    position.Y >= 0 && position.Y < Size.Height)
                {
                    cell = consoleBuffer[position];
                }
                // Render Cursor
                if (ShowCursor && _blinkState)
                {
                    _blinkState = false;
                    if (position.X == _cursorScreenX && position.Y == _cursorScreenY)
                    {
                        if (cell.Content == null || cell.Content == '\0')
                        {
                            return _cursorEmptyCell;
                        }
                        return cell.WithBackground(_cursorBackgroundColor);
                    }
                }



                return cell;
            }
        }
    }
    
    protected override void Render()
    {
        // Assumes lock is held by caller (Initialize or OnInput)
        if (Size.Width <= 0 || Size.Height <= 0) return;
        if (newInput is not null)
        {
            _input = newInput;
            newInput = null;
            ansiConsole.Clear(true);
            var markup = _prompt.Trim();
            ansiConsole.Markup(markup + " ");
            _inputStartX = ansiConsole.CursorX;
            _inputStartY = ansiConsole.CursorY;
            ansiConsole.Write(_input);
            _cursorScreenX = ansiConsole.CursorX;
            _cursorScreenY = ansiConsole.CursorY;
            
            // Auto-scroll frame if present/
            if (Frame is not null)
            {
                var viewportHeight = Frame.ViewportSize.Height;
                if (_cursorScreenY < Frame.Top)
                {
                    Frame.Top = _cursorScreenY;
                }
                else if (_cursorScreenY >= Frame.Top + viewportHeight)
                {
                    Frame.Top = _cursorScreenY - viewportHeight + 1;
                }
            }
        }
       
    }

    protected override void OnPaint(object? sender, UI.PaintEventArgs e)
    {
        if (paintRequests > 0)
        {
            Paint();
        }
        else if (ShowCursor)
        {
            Redraw();
        }
    }

    public override void OnInput(InputEvent inputEvent)
    {
        lock (UI.Lock)
        {
            bool handled = false;
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
            if (newInput is not null)
            {
                Paint();
            }
            if (handled)
            {
                inputEvent.Handled = true;
            }
        }
    }

    private void AttemptCommit()
    {                      
        if (Validator != null)
        {
            var validationResult = Validator(_input);
            if (!validationResult.Successful)
            {
                _validationError = validationResult.Message ?? ValidationErrorMessage;
                Paint();    
                return;
            }
        }

        _validationError = null;
        Committed?.Invoke(this, _input);
    }
    #endregion

    #region Fields
    private readonly string _prompt;
    private readonly StringComparer? _comparer;
    private bool _showCursor;
    private bool _blinkCursor;
    private string _input = string.Empty;
    private string? newInput = null;

    private int _caretPosition = 0;
    private string? _validationError = null;
    private int _inputStartX = 0;
    private int _inputStartY = 0;
    private int _cursorScreenX = 0;
    private int _cursorScreenY = 0;

    private bool _blinkState = true;

    private static readonly ConsoleGUI.Data.Color _cursorBackgroundColor = new ConsoleGUI.Data.Color(100, 100, 100);
    private static readonly Cell _cursorEmptyCell = new Cell(' ').WithBackground(_cursorBackgroundColor);
    #endregion

    #region Events
    public event EventHandler<string>? Committed;
    #endregion
}


