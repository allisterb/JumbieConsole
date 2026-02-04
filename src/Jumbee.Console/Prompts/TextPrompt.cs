namespace Jumbee.Console;

using System;

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
    public override bool HandlesInput => true;

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

    public int CursorX
    {
        get => ansiConsole.CursorX;
        set
        {
            var dx = value - ansiConsole.CursorX;
            ansiConsole.Cursor.MoveRight(dx);
            RenderCursor();
        }
    }

    public int CursorY
    {
        get => ansiConsole.CursorY;
        set
        {
            var dy = value - ansiConsole.CursorY;
            ansiConsole.Cursor.MoveRight(dy);
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
            newInput = false;
        }
        RenderCursor();
    }

    protected void RenderPrompt()
    {
        ansiConsole.Clear(true);
        ansiConsole.Markup(_prompt);
        inputStart = new Position(ansiConsole.CursorX, ansiConsole.CursorY);
    }

    protected void RenderCursor()
    {
        if (IsValidCursorPosition && IsFocused)
        {
            if (_showCursor)
            {
                ansiConsole.Cursor.Show(true);
            }
            else
            {                
                ansiConsole.Cursor.Show(true);                
            }
        }
    }
   
    protected override void Validate()
    {
        if (!_blinkCursor) base.Validate();
    }
  
    protected override void OnInput(InputEvent inputEvent)
    {        
        switch (inputEvent.Key.Key)
        {
            case ConsoleKey.LeftArrow:
                var c = _caretPosition;
                _caretPosition = Math.Max(0, _caretPosition - 1);
                if (c != _caretPosition)
                {
                    --CursorX;
                }
                inputEvent.Handled = true; 
                break;
            case ConsoleKey.RightArrow:
                c = _caretPosition;
                _caretPosition = Math.Min(input.Length, _caretPosition + 1);
                if (c != _caretPosition)
                {
                    ++CursorX;
                }
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

    protected bool IsValidCursorPosition => CursorX < Size.Width && CursorY < Size.Height;

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
    #endregion    
}


