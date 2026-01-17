namespace Jumbee.Console;

using System;
using System.Linq;
using System.Threading;
using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.Space;
using Spectre.Console;

public class TextPrompt : Prompt
{
    #region Constructors
    public TextPrompt(string prompt, bool showCursor = true, bool blinkCursor = true) : base()
    {
        this._prompt = prompt;
        this._showCursor = showCursor;
        this.blinkCursor = blinkCursor;        
    }
    #endregion

    #region Events
    public event EventHandler<string>? Committed;
    #endregion

    #region Properties
    public string Prompt
    {
        get => _prompt;
        set
        {
            _prompt = string.IsNullOrEmpty(value) ? "" : value + " ";
            Invalidate();
        }

    }

    public bool ShowCursor 
    { 
        get => _showCursor; 
        set
        {
            _showCursor = value;
            Invalidate();
        } 
    
    }
    public bool BlinkCursor 
    { 
        get => blinkCursor;

        set
        {
            blinkCursor = value;
            Invalidate();  
        }
    }

    public int CaretPosition
    {
        get => _caretPosition;
        set
        {
            _caretPosition = value;
            DrawCursor();
        }
    }
    #endregion

    #region Methods       
    protected override void Render()
    {
        var (x, y) = consoleBuffer.GetPosition(input.Length + _prompt.Length + 1);
        if ( x == _cursorScreenX && y == _cursorScreenY) 
        {
            return;
        }
       
        ansiConsole.Clear(true);
        ansiConsole.Markup(_prompt);
        inputStart = new Position(ansiConsole.CursorX, ansiConsole.CursorY);
        ansiConsole.Write(input);
    }

    protected void DrawCursor()
    {
        if (!_showCursor) return;
        (_cursorScreenX, _cursorScreenY) = consoleBuffer.AddX(inputStart, _caretPosition);
        if (_showCursor && _cursorScreenX >= 0 && _cursorScreenX < Size.Width &&
                    _cursorScreenY >= 0 && _cursorScreenY < Size.Height)
        {
            var cell = consoleBuffer[_cursorScreenX, _cursorScreenY];
            blinkState = !blinkState;
            if (cell.Content == null || cell.Content == '\0')
            {
                if (blinkCursor && blinkState)
                {
                    consoleBuffer.Write(_cursorScreenX, _cursorScreenY, _cursorEmptyCellBlink);
                }
                else
                {
                    consoleBuffer.Write(_cursorScreenX, _cursorScreenY, _cursorEmptyCell);
                }

            }
            else
            {
                if (blinkCursor && blinkState)
                {
                    consoleBuffer.Write(_cursorScreenX, _cursorScreenY, cell);
                }
                else
                {
                    consoleBuffer.Write(_cursorScreenX, _cursorScreenY, cell.WithBackground(_cursorBackgroundColor)); 
                }
            }
        }
    }

    protected override void Paint()
    {
        Render();
        DrawCursor();
    }

    protected override void Validate()
    {
        if (!blinkCursor) base.Validate();
    }
  
    public override void OnInput(InputEvent inputEvent)
    {        
        switch (inputEvent.Key.Key)
        {
            case ConsoleKey.LeftArrow:
                CaretPosition = Math.Max(0, _caretPosition - 1);
                inputEvent.Handled = true; 
                break;
            case ConsoleKey.RightArrow:
                CaretPosition = Math.Min(input.Length, _caretPosition + 1);
                inputEvent.Handled = true;
                break;
            case ConsoleKey.Home:
                CaretPosition = 0;
                inputEvent.Handled = true;
                break;
            case ConsoleKey.End:
                CaretPosition = input.Length;
                inputEvent.Handled = true;
                break;
            case ConsoleKey.Backspace:
                if (_caretPosition > 0)
                {
                    input = input.Remove(--_caretPosition, 1);
                    Invalidate();
                    inputEvent.Handled = true;
                }
                break;
            case ConsoleKey.Delete:
                if (_caretPosition < input.Length)
                {
                    input = input.Remove(_caretPosition--, 1);
                    Invalidate();
                    inputEvent.Handled = true;
                }
                break;
            case ConsoleKey.Enter:
                AttemptCommit();
                inputEvent.Handled = true;
                break;
            default:
                if (!char.IsControl(inputEvent.Key.KeyChar))
                {
                    input = input.Insert(_caretPosition++, inputEvent.Key.KeyChar.ToString());
                    Invalidate();
                    inputEvent.Handled = true;
                }
                break;
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

    
    #endregion

    #region Fields
    private string _prompt;
    private bool _showCursor;
    private bool blinkCursor;
    private bool blinkState;
    private string input = string.Empty;
    private int _caretPosition = 0;
    private Position inputStart = default;
    private int _cursorScreenX = 0;
    private int _cursorScreenY = 0;
    private static readonly ConsoleGUI.Data.Color _cursorBackgroundColor = new ConsoleGUI.Data.Color(100, 100, 100);
    private static readonly Cell _cursorEmptyCell = new Cell(' ').WithBackground(_cursorBackgroundColor);
    private static readonly Cell _cursorEmptyCellBlink = new Cell(' ');
    #endregion    
}


