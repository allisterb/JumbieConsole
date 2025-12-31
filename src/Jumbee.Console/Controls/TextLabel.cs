namespace Jumbee.Console;

using System.Linq;

using ConsoleGUI.Data;
using ConsoleGUI.Space;

public enum TextLabelOrientation
{
    Horizontal,
    Vertical
}

/// <summary>
/// Displays a single-line text label with a defined horizontal or vertical orientation and foreground and background color.
/// </summary>
public class TextLabel : Control
{
    #region Constructors
    public TextLabel(TextLabelOrientation orientation, string text, Color fgcolor = default, Color bgcolor = default)
    {
        _orientation = orientation;
        _text = text;        
        _fgcolor = fgcolor;
        _bgcolor = bgcolor;
        chars = _text.Select(t => (Cell)new Character(t, foreground: _fgcolor, background: _bgcolor)).ToArray();
        size = orientation == TextLabelOrientation.Horizontal ? new Size(_text.Length, 1) :new Size(1, _text.Length);
        Resize(size);
    }
    #endregion
            
    #region Properties
    public Color FgColor
    {
        get => _fgcolor;
        set
        {
            _fgcolor = value;
            Invalidate();
        }
    }

    public Color BgColor
    {
        get => _bgcolor;
        set
        {
            _bgcolor = value;
            Invalidate();
        }
    }

    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            size = _orientation == TextLabelOrientation.Horizontal ? new Size(_text.Length, 1) : new Size(1, _text.Length);
            Resize(size);
            Invalidate();
        }
    }
    #endregion

    #region Indexers
    public override Cell this[Position position]
    {
        get
        {
            if (string.IsNullOrEmpty(_text))
            {
                return emptyCell;
            }
            if (_orientation == TextLabelOrientation.Horizontal)
            {
                if (position.Y >= 1 || position.X >= Text.Length)
                {
                    return emptyCell;
                }
                else
                {
                    return chars[position.X];
                }
            }
            else
            {
                if (position.X >= 1 || position.Y >= Text.Length)
                {
                    return emptyCell;
                }
                else
                {
                    return chars[position.Y];
                }
            }
        }
    }
    #endregion

    #region Methods
    protected override void Render() => chars = _text.Select(t => (Cell)new Character(t, foreground: _fgcolor, background: _bgcolor)).ToArray();
    
    #endregion

    #region Fields
    private TextLabelOrientation _orientation;
    private string _text = "";
    private Color _fgcolor;
    private Color _bgcolor;
    private Size size;
    private Cell[] chars = [];
    #endregion
}
