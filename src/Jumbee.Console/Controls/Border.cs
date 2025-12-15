namespace Jumbee.Console;

using System;

using ConsoleGUI;
using ConsoleGUI.Common;
using ConsoleGUI.Data;
using ConsoleGUI.Space;
using Spectre.Console.Rendering;

using SpectreBoxBorder = Spectre.Console.BoxBorder;
using SpectreBoxBorderPart = Spectre.Console.Rendering.BoxBorderPart;

public enum BorderStyle
{
    None,
    Ascii,
    Double,
    Heavy,
    Rounded,
    Square
}

public sealed class Border : Control, IDrawingContextListener
{
    #region Constructors
    public Border(IControl content, BorderStyle borderStyle, Color? fgColor = null, Color? bgColor = null)
    {
        _borderStyle = borderStyle switch
        {
            BorderStyle.Ascii => SpectreBoxBorder.Ascii,
            BorderStyle.Double => SpectreBoxBorder.Double,
            BorderStyle.Heavy => SpectreBoxBorder.Heavy,
            BorderStyle.Rounded => SpectreBoxBorder.Rounded,
            BorderStyle.Square => SpectreBoxBorder.Square,
            BorderStyle.None => SpectreBoxBorder.None,
            _ => throw new ArgumentOutOfRangeException(nameof(borderStyle), borderStyle, null)
        };
        Content = content;
        _foreground = fgColor;
        _background = bgColor;
    }
    #endregion

    #region Properties
    public IControl? Content
    {
        get => _content;
        set
        {
            if (_content == value) return;
            _content = value;
            BindContent();
        }
    }

    public BorderPlacement BorderPlacement
    {
        get => _borderPlacement;
        set
        {
            if (_borderPlacement == value) return;
            _borderPlacement = value;
            Initialize();
        }
    }

    public Color? Foreground
    {
        get => _foreground;
        set
        {
            if (Equals(_foreground, value)) return;
            _foreground = value;
            Redraw();
        }
    }

    public Color? Background
    {
        get => _background;
        set
        {
            if (Equals(_background, value)) return;
            _background = value;
            Redraw();
        }
    }

    private DrawingContext ContentContext
    {
        get => _contentContext;
        set
        {
            if (_contentContext == value) return;
            _contentContext?.Dispose();
            _contentContext = value;
            Initialize();
        }
    }
   
   
    #endregion

    #region Indexers
    public override Cell this[Position position]
    {
        get
        {
            if (ContentContext.Contains(position))
                return ContentContext[position];

            if (position.X == 0 && position.Y == 0 && BorderPlacement.HasBorder(BorderPlacement.Top | BorderPlacement.Left))
                return GetCell(BoxBorderPart.TopLeft);

            if (position.X == Size.Width - 1 && position.Y == 0 && BorderPlacement.HasBorder(BorderPlacement.Top | BorderPlacement.Right))
                return GetCell(BoxBorderPart.TopRight);

            if (position.X == 0 && position.Y == Size.Height - 1 && BorderPlacement.HasBorder(BorderPlacement.Bottom | BorderPlacement.Left))
                return GetCell(BoxBorderPart.BottomLeft);

            if (position.X == Size.Width - 1 && position.Y == Size.Height - 1 && BorderPlacement.HasBorder(BorderPlacement.Bottom | BorderPlacement.Right))
                return GetCell(BoxBorderPart.BottomRight);

            if (position.X == 0 && BorderPlacement.HasBorder(BorderPlacement.Left))
                return GetCell(BoxBorderPart.Left);

            if (position.X == Size.Width - 1 && BorderPlacement.HasBorder(BorderPlacement.Right))
                return GetCell(BoxBorderPart.Right);

            if (position.Y == 0 && BorderPlacement.HasBorder(BorderPlacement.Top))
                return GetCell(BoxBorderPart.Top);

            if (position.Y == Size.Height - 1 && BorderPlacement.HasBorder(BorderPlacement.Bottom))
                return GetCell(BoxBorderPart.Bottom);

            return Character.Empty;
        }
    }
    #endregion

    #region Methods
    private Cell GetCell(BoxBorderPart part)
    {
        var str = _borderStyle.GetPart(part);
        var ch = string.IsNullOrEmpty(str) ? ' ' : str[0];
        
        var character = new Character(ch);
        if (_foreground.HasValue) character = character.WithForeground(_foreground.Value);
        if (_background.HasValue) character = character.WithBackground(_background.Value);
        
        return new Cell(character);
    }

    protected override void Initialize()
    {
        using (Freeze())
        {
            var offset = BorderPlacement.AsOffset();
            
            ContentContext?.SetOffset(BorderPlacement.AsVector());

            var minRect = MinSize.AsRect().Remove(offset);
            var maxRect = MaxSize.AsRect().Remove(offset);

            ContentContext?.SetLimits(
                new Size(Math.Max(0, minRect.Width), Math.Max(0, minRect.Height)),
                new Size(Math.Max(0, maxRect.Width), Math.Max(0, maxRect.Height)));

            var contentSize = Content?.Size ?? Size.Empty;
            var sizeRect = contentSize.AsRect().Add(offset);

            Resize(new Size(Math.Max(0, sizeRect.Width), Math.Max(0, sizeRect.Height)));
        }
    }

    private void BindContent()
    {
        if (Content != null)
            ContentContext = new DrawingContext(this, Content);
        else
            ContentContext = DrawingContext.Dummy;
    }

    void IDrawingContextListener.OnRedraw(DrawingContext drawingContext)
    {
        Initialize();
    }

    void IDrawingContextListener.OnUpdate(DrawingContext drawingContext, Rect rect)
    {
        Update(rect);
    }
    #endregion

    #region Fields
    private SpectreBoxBorder _borderStyle;
    private IControl? _content;
    private BorderPlacement _borderPlacement = BorderPlacement.All;
    private Color? _foreground;
    private Color? _background;
    private DrawingContext _contentContext = DrawingContext.Dummy;
    #endregion

}
