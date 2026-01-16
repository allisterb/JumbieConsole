namespace Jumbee.Console;

using System;
using System.Linq;
using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.Space;
using Spectre.Console;

public class TextPrompt : Prompt
{
    #region Constructors
    public TextPrompt(string prompt, StringComparer? comparer = null, bool showCursor = true, bool blinkCursor = true)
    {
        this._prompt = prompt;
        this._comparer = comparer;
        this._showCursor = showCursor;
        this.blinkCursor = blinkCursor;
        this.newInput = "";
    }
    #endregion

    #region Events
    public event EventHandler<string>? Committed;
    #endregion

    #region Properties

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
        get => blinkCursor;

        set
        {
            blinkCursor = value;
            if (!blinkCursor)
            {
                _blinkState = true;
            }
            Invalidate();  
        }
    }

    #endregion

    #region Methods       
    protected override void Render()
    {
        // Assume lock is held by caller (Initialize or OnInput)

        if (newInput is not null)
        {
            input = newInput;
            ansiConsole.Clear(true);
            var markup = _prompt.Trim();
            ansiConsole.Markup(markup + " ");                        
            inputStart = new Position(ansiConsole.CursorX, ansiConsole.CursorY);
            ansiConsole.Write(input);
            _cursorScreenX = ansiConsole.CursorX;
            _cursorScreenY = ansiConsole.CursorY;                       
        }       
    }

    protected override void OnPaint(object? sender, UI.PaintEventArgs e)
    {
        lock (e.Lock)
        {            
            if (paintRequests > 0)
            {
                Paint();
            }
            DrawCursor();
        }                       
    }

    public override void OnInput(InputEvent inputEvent)
    {
        
        bool handled = false;
        newInput = null;
        _blinkState = true;            
        switch (inputEvent.Key.Key)
        {
            case ConsoleKey.LeftArrow:
                _caretPosition = Math.Max(0, _caretPosition - 1);
                handled = true;
                break;
            case ConsoleKey.RightArrow:
                _caretPosition = Math.Min(input.Length, _caretPosition + 1);
                handled = true;
                break;
            case ConsoleKey.Home:
                _caretPosition = 0;
                handled = true;
                break;
            case ConsoleKey.End:
                _caretPosition = input.Length;
                handled = true;
                break;
            case ConsoleKey.Backspace:
                if (_caretPosition > 0)
                {
                    newInput = input.Remove(_caretPosition - 1, 1);
                    _caretPosition--;
                    handled = true;
                }
                break;
            case ConsoleKey.Delete:
                if (_caretPosition < input.Length)
                {
                    newInput = input.Remove(_caretPosition, 1);
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
                    newInput = input.Insert(_caretPosition, inputEvent.Key.KeyChar.ToString());
                    _caretPosition++;
                    handled = true;
                }
                break;
        }
        if (newInput is not null)
        {
            Paint();
        }
        else
        {
            (_cursorScreenX, _cursorScreenY) = consoleBuffer.AddX(inputStart, _caretPosition);   
        }

        if (handled)
        {
            inputEvent.Handled = true;
        }
        
    }

    protected override void Control_OnFocus()
    {
        BlinkCursor = true;

    }

    protected override void Control_OnLostFocus()
    {
        BlinkCursor = false;    
    }

    private void AttemptCommit()
    {                      

        Committed?.Invoke(this, input);
    }

    protected void DrawCursor()
    {
        _blinkState = !_blinkState;
        if (ShowCursor && _blinkState && _cursorScreenX >= 0 && _cursorScreenX < Size.Width &&
                    _cursorScreenY >= 0 && _cursorScreenY < Size.Height)
        {
            var cell = consoleBuffer[_cursorScreenX, _cursorScreenY];
            if (cell.Content == null || cell.Content == '\0')
            {
                consoleBuffer.Write(_cursorScreenX, _cursorScreenY, _cursorEmptyCell);
            }
            else
            {
                consoleBuffer.Write(_cursorScreenX, _cursorScreenY, cell.WithBackground(_cursorBackgroundColor));
                
            }
        }
    }
    #endregion

    #region Fields
    private readonly string _prompt;
    private readonly StringComparer? _comparer;
    private bool _showCursor;
    private bool blinkCursor;
    private string input = string.Empty;
    private string? newInput = null;

    private int _caretPosition = 0;
    private Position inputStart = default;
    private int _cursorScreenX = 0;
    private int _cursorScreenY = 0;

    private bool _blinkState = true;

    private static readonly ConsoleGUI.Data.Color _cursorBackgroundColor = new ConsoleGUI.Data.Color(100, 100, 100);
    private static readonly Cell _cursorEmptyCell = new Cell(' ').WithBackground(_cursorBackgroundColor);
    #endregion    
}


