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
    }

    protected override void Render()
    {
        if (newInput)
        {            
            write(input);
            newInput = false;
        }
        RenderCursor();
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

    public override void OnInput(InputEvent inputEvent)
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
                input = input.Insert(_caretPosition++, "\n");
                newInput = true;
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
