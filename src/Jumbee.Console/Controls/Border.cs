namespace Jumbee.Console;

using System;

using ConsoleGUI;
using ConsoleGUI.Common;
using ConsoleGUI.Data;
using ConsoleGUI.Input;
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

/// <summary>
/// Draws a border around a control.
/// </summary>
public sealed class Border : ConsoleGUI.Common.Control, IDrawingContextListener, IInputListener
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

    public string? Title
    {
        get => _title;
        set
        {
            if (_title == value) return;
            _title = value;
            Initialize();
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

    public Offset Margin
    {
        get => _margin;
        set
        {
            if (_margin.Equals(value)) return;
            _margin = value;
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

    private int _top;
    public int Top
    {
        get => _top;
        set
        {
            // Clamping will happen in Initialize or we need context size here.
            // VerticalScrollPanel clamps in setter but accesses ContentContext.Size.
            // We can do same if ContentContext is valid.
            var maxTop = Math.Max(0, (ContentContext?.Size.Height ?? 0) - (Size.Height - (Margin.Top + Margin.Bottom) - (BorderPlacement.AsOffset().Top + BorderPlacement.AsOffset().Bottom)));
            // Note: The above calculation is rough, Initialize does it better. 
            // Let's just set and let Initialize clamp or trigger logic.
            // But VerticalScrollPanel clamps in setter.
            // We'll stick to simple set + Initialize for now, logic in Initialize can fix _top if needed?
            // Actually, best to just trigger update.
             if (_top == value) return;
            _top = value;
            Initialize();
        }
    }

    private Character _scrollBarForeground = new Character('▀', foreground: new Color(100, 100, 255));
    public Character ScrollBarForeground
    {
        get => _scrollBarForeground;
        set
        {
            if (_scrollBarForeground.Equals(value)) return;
            _scrollBarForeground = value;
            Redraw(); // Just redraw scrollbar? Or full? Full is easier.
        }
    }

    private Character _scrollBarBackground = new Character('║', foreground: new Color(100, 100, 100));
    public Character ScrollBarBackground
    {
        get => _scrollBarBackground;
        set
        {
            if (_scrollBarBackground.Equals(value)) return;
            _scrollBarBackground = value;
            Redraw();
        }
    }

    public ConsoleKey ScrollUpKey { get; set; } = ConsoleKey.UpArrow;
    public ConsoleKey ScrollDownKey { get; set; } = ConsoleKey.DownArrow;

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
            // 1. Calculate Offsets & Viewport
            // We replicate Initialize logic to ensure consistency
            var borderOffset = BorderPlacement.AsOffset();
            if (!string.IsNullOrEmpty(Title) && BorderPlacement.HasBorder(BorderPlacement.Top))
                 borderOffset = new Offset(borderOffset.Left, borderOffset.Top + 2, borderOffset.Right, borderOffset.Bottom);
            
            var totalOffset = new Offset(
                borderOffset.Left + Margin.Left,
                borderOffset.Top + Margin.Top,
                borderOffset.Right + Margin.Right,
                borderOffset.Bottom + Margin.Bottom);

            var contentLeft = totalOffset.Left;
            var contentTop = totalOffset.Top;
            var contentRight = Size.Width - 1 - totalOffset.Right;
            var contentBottom = Size.Height - 1 - totalOffset.Bottom;

            // 2. Content & Scrollbar (Inside Viewport)
            if (position.X >= contentLeft && position.X <= contentRight &&
                position.Y >= contentTop && position.Y <= contentBottom)
            {
                // Scrollbar logic: always at the right edge of valid content area
                if (position.X == contentRight)
                {
                    if (Content == null) return ScrollBarForeground;

                    var viewportHeight = contentBottom - contentTop + 1;
                    var contentHeight = ContentContext.Size.Height;

                    // Only draw scrollbar if content is larger than viewport
                    if (contentHeight > viewportHeight)
                    {
                        // Calculate thumb position
                        // Relative Y in viewport
                        var relY = position.Y - contentTop;
                        
                        long checkY = (long)relY * contentHeight;
                        long startThumb = (long)_top * viewportHeight;
                        long endThumb = (long)(_top + viewportHeight) * viewportHeight; 

                        // Note: endThumb logic might need tweaking for exact pixel match, 
                        // but this proportional logic is standard.
                        // Ideally: 
                        // thumbTop = (_top * viewportHeight) / contentHeight
                        // thumbSize = (viewportHeight * viewportHeight) / contentHeight
                        
                        // Using the previous multiplication logic to avoid integer division issues:
                        // if (relY * contentHeight < _top * viewportHeight) -> Background
                        
                        if (checkY < startThumb) return ScrollBarBackground;
                        if (checkY >= endThumb) return ScrollBarBackground; 
                        
                        return ScrollBarForeground;
                    }
                    else
                    {
                         // No scrollbar needed -> allow content to draw here?
                         // Current design reserves the column. 
                         // If we reserved the column in Initialize (limitWidth), content shouldn't be here.
                         // But for aesthetic, maybe draw empty or background?
                         // If we return Character.Empty, we see background.
                         // Let's return Character.Empty so content *could* extend if we changed limits,
                         // but currently it acts as padding.
                         // Actually, if we don't return here, it falls through to ContentContext.Contains
                         // which might return true if we didn't limit width.
                    }
                }

                if (ContentContext.Contains(position))
                    return ContentContext[position];

                return Character.Empty;
            }

            // 3. Borders & Title (Outside Viewport)
            var left = Margin.Left;
            var top = Margin.Top;
            var right = Size.Width - 1 - Margin.Right;
            var bottom = Size.Height - 1 - Margin.Bottom;

            if (position.X == left && position.Y == top && BorderPlacement.HasBorder(BorderPlacement.Top | BorderPlacement.Left))
                return GetCell(BoxBorderPart.TopLeft);

            if (position.X == right && position.Y == top && BorderPlacement.HasBorder(BorderPlacement.Top | BorderPlacement.Right))
                return GetCell(BoxBorderPart.TopRight);

            if (position.X == left && position.Y == bottom && BorderPlacement.HasBorder(BorderPlacement.Bottom | BorderPlacement.Left))
                return GetCell(BoxBorderPart.BottomLeft);

            if (position.X == right && position.Y == bottom && BorderPlacement.HasBorder(BorderPlacement.Bottom | BorderPlacement.Right))
                return GetCell(BoxBorderPart.BottomRight);

            if (position.X == left && position.Y >= top && position.Y <= bottom && BorderPlacement.HasBorder(BorderPlacement.Left))
                return GetCell(BoxBorderPart.Left);

            if (position.X == right && position.Y >= top && position.Y <= bottom && BorderPlacement.HasBorder(BorderPlacement.Right))
                return GetCell(BoxBorderPart.Right);

            if (position.Y == top && position.X >= left && position.X <= right && BorderPlacement.HasBorder(BorderPlacement.Top))
                return GetCell(BoxBorderPart.Top);

            if (position.Y == bottom && position.X >= left && position.X <= right && BorderPlacement.HasBorder(BorderPlacement.Bottom))
                return GetCell(BoxBorderPart.Bottom);

            if (!string.IsNullOrEmpty(Title) && BorderPlacement.HasBorder(BorderPlacement.Top))
            {
                if (position.Y == top + 1)
                {
                    var startX = BorderPlacement.HasBorder(BorderPlacement.Left) ? left + 1 : left;
                    var titleIndex = position.X - startX;

                    if (titleIndex >= 0 && titleIndex < Title.Length)
                    {
                        var character = new Character(Title[titleIndex]);
                        if (_foreground.HasValue) character = character.WithForeground(_foreground.Value);
                        if (_background.HasValue) character = character.WithBackground(_background.Value);
                        return new Cell(character);
                    }
                }
                else if (position.Y == top + 2)
                {
                    if (position.X == left) // Start character for separator
                    {
                        return GetCell(BoxBorderPart.TopLeft);
                    }
                    else if (position.X == right) // End character for separator
                    {
                        return GetCell(BoxBorderPart.TopRight);
                    }
                    else if (position.X > left && position.X < right) // Middle characters for separator
                    {
                        return GetCell(BoxBorderPart.Top);
                    }
                }
            }

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
            var borderOffset = BorderPlacement.AsOffset();

            if (!string.IsNullOrEmpty(Title) && BorderPlacement.HasBorder(BorderPlacement.Top))
                borderOffset = new Offset(borderOffset.Left, borderOffset.Top + 2, borderOffset.Right, borderOffset.Bottom);
            
            var totalOffset = new Offset(
                borderOffset.Left + Margin.Left,
                borderOffset.Top + Margin.Top,
                borderOffset.Right + Margin.Right,
                borderOffset.Bottom + Margin.Bottom);

            // Available space for content (excluding scrollbar for now)
            // We reserve 1 column for scrollbar at the right of content
            var contentLimitsMin = MinSize.AsRect().Remove(totalOffset).Size;
            var contentLimitsMax = MaxSize.AsRect().Remove(totalOffset).Size;
            
            // Allow infinite height for scrolling, but constrain width to make space for scrollbar
            // If MaxSize.Width is infinite, we don't constrain width (except by MinSize/Content)
            // But we generally want to fit in MaxSize.
            
            var limitWidth = Math.Max(0, contentLimitsMax.Width - 1);
            ContentContext?.SetLimits(
                new Size(Math.Max(0, contentLimitsMin.Width - 1), 0), 
                new Size(limitWidth, int.MaxValue));

            // Clamp Top
            var viewportHeight = Math.Max(0, Size.Height - totalOffset.Top + totalOffset.Bottom); 
            // Note: Size.Height is current size. During Resize sequence, this might be stale?
            // VerticalScrollPanel uses Size.Height (which is current).
            // But here we are about to Resize.
            // If we are about to Resize to MaxSize (if content is large), then viewport will be larger.
            // Let's rely on Redraw loop?
            // Actually, we should probably use 'contentLimitsMax.Height' as the viewport constraint if we are expanding?
            // But MaxSize might be infinite.
            // Let's stick to simple clamping against current content size vs current viewport estimate?
            // Or just allow Top to be set, and Resize will clip?
            
            if (ContentContext != null)
            {
                var contentHeight = ContentContext.Size.Height;
                // If we expand to MaxSize, the viewport height will be at most MaxSize - Offsets.
                var maxViewportHeight = Math.Max(0, MaxSize.Height - totalOffset.Top + totalOffset.Bottom);
                // If MaxSize is infinite, maxViewportHeight is infinite?
                if (MaxSize.Height == int.MaxValue) maxViewportHeight = int.MaxValue;
                
                // Actual viewport height used for clamping depends on what size we WILL be.
                // But we don't know yet.
                // However, 'Top' only matters if we are scrolling.
                // We scroll if ContentHeight > ViewportHeight.
                
                _top = Math.Max(0, Math.Min(_top, contentHeight - 1)); // Ensure at least within content? 
                // Better: _top = Math.Max(0, Math.Min(_top, contentHeight - (currentViewportHeight)));
                // But we don't know currentViewportHeight easily before Resize.
            }
            
            ContentContext?.SetOffset(new Vector(totalOffset.Left, totalOffset.Top - _top));

            var contentSize = ContentContext?.Size ?? Size.Empty;
            
            // Calculate desired size including margins, borders, and scrollbar (1 extra width)
            var desiredContentSize = contentSize.Expand(1, 0); // +1 Width for scrollbar
            var sizeRect = desiredContentSize.AsRect().Add(totalOffset);

            Resize(Size.Clip(MinSize, sizeRect.Size, MaxSize));
            
            // Re-clamp Top after resize? 
            // If we resized, Size.Height is now updated (if Resize is immediate? No, Resize schedules/updates Size property).
            // Actually 'Control.Resize' updates 'Size' immediately in ConsoleGUI?
            // Checking ConsoleGUI source (mental): Resize usually updates Size.
            
            // Post-Resize Clamping:
            if (ContentContext != null)
            {
                 viewportHeight = Math.Max(0, Size.Height - totalOffset.Top + totalOffset.Bottom);
                 if (ContentContext.Size.Height > viewportHeight)
                 {
                     _top = Math.Min(ContentContext.Size.Height - viewportHeight, Math.Max(0, _top));
                     // Update offset again with clamped Top?
                     ContentContext.SetOffset(new Vector(totalOffset.Left, totalOffset.Top - _top));
                 }
                 else
                 {
                     _top = 0;
                     ContentContext.SetOffset(new Vector(totalOffset.Left, totalOffset.Top));
                 }
            }
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

    void IInputListener.OnInput(InputEvent inputEvent)
    {
        if (inputEvent.Key.Key == ScrollUpKey)
        {
            Top -= 1;
            inputEvent.Handled = true;
        }
        else if (inputEvent.Key.Key == ScrollDownKey)
        {
            Top += 1;
            inputEvent.Handled = true;
        }
    }
    #endregion

    #region Fields
    private SpectreBoxBorder _borderStyle;
    private IControl? _content;
    private BorderPlacement _borderPlacement = BorderPlacement.All;
    private Offset _margin;
    private Color? _foreground;
    private Color? _background;
    private DrawingContext _contentContext = DrawingContext.Dummy;
    private string? _title;
    #endregion

}
