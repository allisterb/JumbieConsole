namespace Jumbee.Console;

using System;
using System.IO;
using System.Threading.Tasks;
using RazorConsole.Core;

using ConsoleGUI.Input;
using ConsoleGUI.Space;
using NTokenizers.CSharp;
using NTokenizers.Extensions.Spectre.Console;
using NTokenizers.Extensions.Spectre.Console.Styles;
using NTokenizers.Extensions.Spectre.Console.Writers;
using NTokenizers.Markdown;
using Spectre.Console;
using RazorConsole.Core.Rendering.Syntax;
using ColorCode;

/// <summary>
/// A text editor control with syntax highlighting for supported languages.
/// </summary>
public class TextEditor : Control
{
    #region Constructors
    public TextEditor(Language language = Language.None, bool showCursor = true, bool blinkCursor = false) : base()
    {
        this._language = language;
        this._showCursor = showCursor;
        this._blinkCursor = blinkCursor;        
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
            WriteText(_language, input);
            newInput = false;
        }
        RenderCursor();
    }
    
    protected void RenderCursor()
    {
        var pos = GetCursorPosition(caretPosition);
        ansiConsole.SetCursorPosition(pos.x, pos.y);        
        if (IsFocused && _showCursor)
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

    protected override void Validate()
    {
        if (!_blinkCursor) base.Validate();
    }

    protected override void Control_OnInitialization()
    {
        if (!string.IsNullOrEmpty(input))
        {
            ansiConsole.Clear(true);
            WriteText(_language, input);
            Validate();
        }        
    }

    protected override void OnInput(InputEvent inputEvent)
    {
        switch (inputEvent.Key.Key)
        {
            case ConsoleKey.LeftArrow:
                if (caretPosition > 0)
                {
                    caretPosition--;
                    UpdateDesiredColumn();
                }
                inputEvent.Handled = true;
                break;
            case ConsoleKey.RightArrow:
                if (caretPosition < input.Length)
                {
                    caretPosition++;
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
            case ConsoleKey.PageUp:
                MoveCaretVertically(-(Frame?.ViewportSize.Height ?? 10));
                inputEvent.Handled = true;
                break;
            case ConsoleKey.PageDown:
                MoveCaretVertically(Frame?.ViewportSize.Height ?? 10);
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
                if (caretPosition > 0)
                {
                    input = input.Remove(--caretPosition, 1);
                    newInput = true;
                    UpdateDesiredColumn();
                    inputEvent.Handled = true;
                }
                break;
            case ConsoleKey.Delete:
                if (caretPosition < input.Length)
                {
                    input = input.Remove(caretPosition, 1);
                    newInput = true;
                    UpdateDesiredColumn();
                    inputEvent.Handled = true;
                }
                break;
            case ConsoleKey.Enter:
                input = input.Insert(caretPosition++, "\n");
                newInput = true;
                UpdateDesiredColumn();
                inputEvent.Handled = true;
                break;
            default:
                if (!char.IsControl(inputEvent.Key.KeyChar))
                {
                    input = input.Insert(caretPosition++, inputEvent.Key.KeyChar.ToString());
                    newInput = true;

                    UpdateDesiredColumn();
                    inputEvent.Handled = true;
                }
                break;
        }
        AutoScroll();
        Invalidate();
    }

    private void AutoScroll()
    {
        if (Frame != null)
        {
            var (x, y) = GetCursorPosition(caretPosition);
            
            int top = Frame.Top;
            int viewportHeight = Frame.ViewportSize.Height;
            
            if (y < top)
            {
                Frame.Top = y;
            }
            else if (y >= top + viewportHeight)
            {
                Frame.Top = y - viewportHeight + 1;
            }
        }
    }

    private void UpdateDesiredColumn()
    {
        var pos = GetCursorPosition(caretPosition);
        _desiredColumn = pos.x;
    }

    private void MoveCaretVertically(int direction)
    {
        var (currentX, currentY) = GetCursorPosition(caretPosition);
        var targetY = Math.Max(0, currentY + direction);
        
        caretPosition = GetCaretIndex(targetY, _desiredColumn);
    }
    
    private void MoveCaretHome()
    {
        int i = caretPosition;
        while (i > 0 && input[i-1] != '\n')
        {
            i--;
        }
        caretPosition = i;
    }

    private void MoveCaretEnd()
    {
         while (caretPosition < input.Length && input[caretPosition] != '\n')
         {
             caretPosition++;
         }
    }

    private (int x, int y) GetCursorPosition(int caret)
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
    
    private void WriteText(Language language, string text)
    {
        switch (language)
        {
            case Language.None:
                ansiConsole.Write(text); 
                break;
            case Language.Markdown:
                ansiConsole.Write(new Markup(ccFormatter.Format(text, Languages.Markdown, ccSyntaxTheme, ccSyntaxOptions)));
                break;
            case Language.CSharp:
                ansiConsole.Markup(ccFormatter.Format(text, Languages.CSharp, ccSyntaxTheme, ccSyntaxOptions));
                break;
            case Language.TypeScript:
                ansiConsole.Markup(ccFormatter.Format(text, Languages.Typescript, ccSyntaxTheme, ccSyntaxOptions));
                break;
            case Language.Sql:
                ansiConsole.Markup(ccFormatter.Format(text, Languages.Sql, ccSyntaxTheme, ccSyntaxOptions));
                break;
            case Language.Json:
                ansiConsole.WriteJson(text);
                break;
            case Language.Html:
                ansiConsole.Markup(ccFormatter.Format(text, Languages.Html, ccSyntaxTheme, ccSyntaxOptions));
                break;
            case Language.Css:
                ansiConsole.Markup(ccFormatter.Format(text, Languages.Css, ccSyntaxTheme, ccSyntaxOptions));
                break;
            case Language.Xml:
                ansiConsole.Markup(ccFormatter.Format(text, Languages.Xml, ccSyntaxTheme, ccSyntaxOptions));
                break;
            case Language.Yaml:
                ansiConsole.WriteYaml(text);
                break;
        }
    }
    #endregion

    #region Fields
    private int _desiredColumn = 0;
    private Language _language;
    private bool _showCursor;
    private bool _blinkCursor;
    private bool blinkState;
    private string input = string.Empty;
    private bool newInput;
    private int caretPosition = 0;

    SpectreMarkupFormatter ccFormatter = new SpectreMarkupFormatter() ;
    SyntaxTheme ccSyntaxTheme = SyntaxTheme.CreateDefault();
    SyntaxOptions ccSyntaxOptions = new SyntaxOptions() { TabWidth = 0,   };    
    #endregion

    #region Types
    public enum Language
    {
        None,
        Markdown,
        Json,
        Html,
        Css,
        CSharp,
        Sql,
        TypeScript,
        Xml,
        Yaml
    }   
    #endregion
}
