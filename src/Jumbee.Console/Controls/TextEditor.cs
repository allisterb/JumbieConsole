namespace Jumbee.Console;

using System;

using ConsoleGUI.Input;
using ConsoleGUI.Space;
using Spectre.Console;
using NTokenizers.Extensions.Spectre.Console;

public class TextEditor : Control
{
    #region Constructors
    public TextEditor(Language language = Language.None, bool showCursor = true, bool blinkCursor = false) : base()
    {
        this._language = language;
        this._showCursor = showCursor;
        this._blinkCursor = blinkCursor;
        this.write = GetLanguageWriter(language);
    }
    #endregion

    #region Properties
    public override bool HandlesInput => true;

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
    
    public int CursorX
    {
        get => ansiConsole.CursorX;
        set
        {
            var x = Math.Clamp(value, 0, ActualWidth);
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
            var y = Math.Clamp(value, 0, ActualHeight);
            var dy = y - ansiConsole.CursorY;
            ansiConsole.Cursor.MoveDown(dy);
            RenderCursor();
        }
    }
    #endregion

    #region Methods                   
    protected override void Render()
    {
        if (newInput)
        {
            ansiConsole.Clear(true);
            write(input);
            newInput = false;
        }
        RenderCursor();
    }
    
    protected void RenderCursor()
    {
        var pos = CalculateCursorPosition(_caretPosition);
        ansiConsole.SetCursorPosition(pos.x, pos.y);
        
        if (IsFocused && _showCursor)
        {
            ansiConsole.Cursor.Show();
        }

        else
        {
            ansiConsole.Cursor.Hide();
        }        
    }

    protected override void Validate()
    {
        if (!_blinkCursor) base.Validate();
    }

    protected override void Control_OnInitialization()
    {
        if (!string.IsNullOrEmpty(input))
        {
            ansiConsole.Clear(true);
            write(input);
            Validate();
        }        
    }

    protected override void OnInput(InputEvent inputEvent)
    {
        switch (inputEvent.Key.Key)
        {
            case ConsoleKey.LeftArrow:
                if (_caretPosition > 0)
                {
                    _caretPosition--;
                    UpdateDesiredColumn();
                }
                inputEvent.Handled = true;
                break;
            case ConsoleKey.RightArrow:
                if (_caretPosition < input.Length)
                {
                    _caretPosition++;
                    UpdateDesiredColumn();
                }
                inputEvent.Handled = true;
                break;
            case ConsoleKey.UpArrow:
                MoveCaretVertically(-1);
                inputEvent.Handled = true;
                break;
            case ConsoleKey.DownArrow:
                MoveCaretVertically(1);
                inputEvent.Handled = true;
                break;
            case ConsoleKey.Home:
                MoveCaretHome();
                UpdateDesiredColumn();
                inputEvent.Handled = true;
                break;
            case ConsoleKey.End:
                MoveCaretEnd();
                UpdateDesiredColumn();
                inputEvent.Handled = true;
                break;
            case ConsoleKey.Backspace:
                if (_caretPosition > 0)
                {
                    input = input.Remove(--_caretPosition, 1);
                    newInput = true;
                    UpdateDesiredColumn();
                    inputEvent.Handled = true;
                }
                break;
            case ConsoleKey.Delete:
                if (_caretPosition < input.Length)
                {
                    input = input.Remove(_caretPosition, 1);
                    newInput = true;
                    UpdateDesiredColumn();
                    inputEvent.Handled = true;
                }
                break;
            case ConsoleKey.Enter:
                input = input.Insert(_caretPosition++, "\n");
                newInput = true;
                UpdateDesiredColumn();
                inputEvent.Handled = true;
                break;
            default:
                if (!char.IsControl(inputEvent.Key.KeyChar))
                {
                    input = input.Insert(_caretPosition++, inputEvent.Key.KeyChar.ToString());
                    newInput = true;
                    UpdateDesiredColumn();
                    inputEvent.Handled = true;
                }
                break;
        }
        Invalidate();
    }

    private void UpdateDesiredColumn()
    {
        var pos = CalculateCursorPosition(_caretPosition);
        _desiredColumn = pos.x;
    }

    private void MoveCaretVertically(int direction)
    {
        var (currentX, currentY) = CalculateCursorPosition(_caretPosition);
        var targetY = currentY + direction;
        
        if (targetY < 0) return;
        
        _caretPosition = GetCaretIndex(targetY, _desiredColumn);
    }
    
    private void MoveCaretHome()
    {
        int i = _caretPosition;
        while (i > 0 && input[i-1] != '\n')
        {
            i--;
        }
        _caretPosition = i;
    }

    private void MoveCaretEnd()
    {
         while (_caretPosition < input.Length && input[_caretPosition] != '\n')
         {
             _caretPosition++;
         }
    }

    private (int x, int y) CalculateCursorPosition(int caret)
    {
        int x = 0;
        int y = 0;
        for (int i = 0; i < caret && i < input.Length; i++)
        {
            char c = input[i];
            if (c == '\n')
            {
                x = 0;
                y++;
            }
            else if (c == '\r') continue;
            else
            {
                x += c.GetCellWidth();
            }
        }
        return (x, y);
    }

    private int GetCaretIndex(int targetLine, int targetX)
    {
        int line = 0;
        int currentX = 0;
        int i = 0;
        
        while (i < input.Length && line < targetLine)
        {
            if (input[i] == '\n') line++;
            i++;
        }
        
        if (line < targetLine) return input.Length;
        
        while (i < input.Length && input[i] != '\n')
        {
            if (input[i] == '\r') 
            {
                i++; 
                continue;
            }
            
            int w = input[i].ToString().GetCellWidth();
            
            if (currentX + (w / 2.0) > targetX) break;
            
            currentX += w;
            i++;
            
            if (currentX >= targetX) break;
        }
        
        return i;
    }

    protected bool IsValidCursorPosition => CursorX < Size.Width && CursorY < Size.Height;

    protected Action<string> GetLanguageWriter(Language language) => language switch
    {
        Language.None => ansiConsole.Write,
        Language.CSharp => ansiConsole.WriteCSharp,
        Language.Sql => ansiConsole.WriteSql,
        Language.Markdown => ansiConsole.WriteMarkdown,
        Language.Json => ansiConsole.WriteJson,
        Language.Html => ansiConsole.WriteHtml,
        Language.Css => ansiConsole.WriteCss,
        Language.TypeScript => ansiConsole.WriteTypescript,
        Language.Xml => ansiConsole.WriteXml,
        Language.Yaml => ansiConsole.WriteYaml,
        _ => throw new NotImplementedException()
    };
    #endregion

    #region Fields
    private int _desiredColumn = 0;
    private Language _language;
    private bool _showCursor;
    private bool _blinkCursor;
    private bool blinkState;
    private string input = string.Empty;
    private bool newInput;
    private int _caretPosition = 0;
    private Action<string> write;
    #endregion

    #region Types
    public enum Language
    {
        None,
        Markdown,
        CSharp,
        Sql,
        Json,
        Html,
        Css,
        TypeScript,
        Xml,
        Yaml
    }

    #endregion
}
