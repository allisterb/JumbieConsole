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
    public TextPrompt(string prompt, bool showCursor = true, bool blinkCursor = false) : base()
    {
        this._prompt = prompt;
        this._showCursor = showCursor;
        this._blinkCursor = blinkCursor;
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
        get => _blinkCursor;

        set
        {
            _blinkCursor = value;
            Invalidate();  
        }
    }

    public int CaretPosition
    {
        get => _caretPosition;
        set
        {
            _caretPosition = value;
            RenderCursor();
        }
    }

    public int CursorScreenX
    {
        get => _cursorScreenX;
        set
        {
            _cursorScreenX = ClampWidth(value);
            RenderCursor();
        }
    }

    public int CursorScreenY
    {
        get => _cursorScreenY;
        set
        {
            _cursorScreenY = ClampHeight(value);
            RenderCursor();
        }
    }
    #endregion

    #region Methods       
    protected override void Initialize()
    {
        base.Initialize();
        RenderPrompt();
    }

    protected override void Render()
    {
        if (newInput)
        {
            RenderPrompt();
            ansiConsole.Write(Markup.Escape(input));
            _cursorScreenX = ansiConsole.CursorX;
            _cursorScreenY = ansiConsole.CursorY;            
            newInput = false;
        }
        RenderCursor();
    }

    protected void RenderPrompt()
    {
        ansiConsole.Clear(true);
        ansiConsole.Markup(_prompt);
        inputStart = new Position(ansiConsole.CursorX, ansiConsole.CursorY);
        _cursorScreenX = ansiConsole.CursorX;
        _cursorScreenY = ansiConsole.CursorY;
    }

    protected void RenderCursor()
    {
        if (IsValidCursorPosition && IsFocused)
        {
            if (_showCursor)
            {
                if (_blinkCursor)
                {                  
                    ansiConsole.Cursor.Show(blinkState = !blinkState);
                }
                else 
                {
                    ansiConsole.Cursor.Show();
                }
            }
            else
            {
                ansiConsole.Cursor.Hide();
            }
        }
    }

   
    protected override void Validate()
    {
        if (!_blinkCursor) base.Validate();
    }
  
    public override void OnInput(InputEvent inputEvent)
    {        
        switch (inputEvent.Key.Key)
        {
            case ConsoleKey.LeftArrow:
                _caretPosition = Math.Max(0, _caretPosition - 1);
                --CursorScreenX;
                inputEvent.Handled = true; 
                break;
            case ConsoleKey.RightArrow:
                _caretPosition = Math.Min(input.Length, _caretPosition + 1);
                ++CursorScreenX;
                inputEvent.Handled = true;
                break;
            case ConsoleKey.Home:
                _caretPosition = 0;
                inputEvent.Handled = true;
                break;
            case ConsoleKey.End:
                _caretPosition = input.Length;
                inputEvent.Handled = true;
                break;
            case ConsoleKey.Backspace:
                if (_caretPosition > 0)
                {
                    input = input.Remove(--_caretPosition, 1);
                    newInput = true;    
                    inputEvent.Handled = true;
                }
                break;
            case ConsoleKey.Delete:
                if (_caretPosition < input.Length)
                {
                    input = input.Remove(_caretPosition--, 1);
                    newInput = true;

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
                    newInput = true;
                    inputEvent.Handled = true;
                }
                break;
        }
        Invalidate();
    }

    protected bool IsValidCursorPosition => _cursorScreenX < Size.Width && _cursorScreenY < Size.Height;

    private void AttemptCommit()
    {                      

        Committed?.Invoke(this, input);
    }

    
    #endregion

    #region Fields
    private string _prompt;
    private bool _showCursor;
    private bool _blinkCursor;    
    private bool blinkState;
    private string input = string.Empty;
    private bool newInput;
    private int _caretPosition = 0;
    private Position inputStart = default;
    private int _cursorScreenX = 0;
    private int _cursorScreenY = 0;

    #endregion    
}


