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
/// Draws a border around a control together with margins and a title bar.
/// </summary>
public sealed class ControlFrame : ConsoleGUI.Common.Control, IDrawingContextListener, IInputListener
{
    #region Constructors
    public ControlFrame(Control control, BorderStyle? borderStyle = null, Offset? margin = null, Color? fgColor = null, Color? bgColor = null, string? title=null)
    {
        _borderStyle = borderStyle ?? BorderStyle.None; 
        _boxBorder = GetSpectreBoxBorder(_borderStyle);
        _margin = margin ?? DefaultMargin;
        _foreground = fgColor;
        _background = bgColor;
        _title = title;
        _control = control;
        BindControl();
    }
    #endregion

    #region Properties
    public Control Control
    {
        get => _control;
        set
        {
            
            _control = value;
            BindControl();
        }
    }

    public BorderStyle BorderStyle
    {
        get => _borderStyle;
        set
        {
            if (_borderStyle == value) return;
            _borderStyle = value;
            _boxBorder = GetSpectreBoxBorder(_borderStyle);
            Initialize();
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
   
    public int Top
    {
        get => _top;
        set
        {            
            // Let's just set and let Initialize clamp or trigger logic.
            _top = value;
            Initialize();
        }
    }
  
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

    private DrawingContext ControlContext
    {
        get => _controlContext;
        set
        {
            if (_controlContext == value) return;
            _controlContext?.Dispose();
            _controlContext = value;
            Initialize();
        }
    }      
    #endregion

    #region Indexers
    public override Cell this[Position position]
    {
        get
        {
            lock (UI.Lock)
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

                var controlLeft = totalOffset.Left;
                var controlTop = totalOffset.Top;
                var controlRight = Size.Width - 1 - totalOffset.Right;
                var controlBottom = Size.Height - 1 - totalOffset.Bottom;

                // 2. Control & Scrollbar (Inside Viewport)
                if (position.X >= controlLeft && position.X <= controlRight &&
                    position.Y >= controlTop && position.Y <= controlBottom)
                {
                    // Scrollbar logic: always at the right edge of valid control area
                    if (position.X == controlRight)
                    {
                        if (Control == null) return ScrollBarForeground;

                        var viewportHeight = controlBottom - controlTop + 1;
                        var controlHeight = ControlContext.Size.Height;

                        // Only draw scrollbar if control is larger than viewport
                        if (controlHeight > viewportHeight)
                        {
                            // Calculate thumb position
                            // Relative Y in viewport
                            var relY = position.Y - controlTop;
                            
                            long checkY = (long)relY * controlHeight;
                            long startThumb = (long)_top * viewportHeight;
                            long endThumb = (long)(_top + viewportHeight) * viewportHeight; 

                            // Note: endThumb logic might need tweaking for exact pixel match, 
                            // but this proportional logic is standard.
                            // Ideally: 
                            // thumbTop = (_top * viewportHeight) / controlHeight
                            // thumbSize = (viewportHeight * viewportHeight) / controlHeight
                            
                            // Using the previous multiplication logic to avoid integer division issues:
                            // if (relY * controlHeight < _top * viewportHeight) -> Background
                            
                            if (checkY < startThumb) return ScrollBarBackground;
                            if (checkY >= endThumb) return ScrollBarBackground; 
                            
                            return ScrollBarForeground;
                        }
                        else
                        {
                             // No scrollbar needed -> allow control to draw here?
                             // Current design reserves the column. 
                             // If we reserved the column in Initialize (limitWidth), control shouldn't be here.
                             // But for aesthetic, maybe draw empty or background?
                             // If we return Character.Empty, we see background.
                             // Let's return Character.Empty so control *could* extend if we changed limits,
                             // but currently it acts as padding.
                             // Actually, if we don't return here, it falls through to ControlContext.Contains
                             // which might return true if we didn't limit width.
                        }
                    }

                    if (ControlContext.Contains(position))
                        return ControlContext[position];

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
    }
    #endregion

    #region Methods
    public static SpectreBoxBorder GetSpectreBoxBorder(BorderStyle style)
    {
        return style switch
        {
            BorderStyle.Ascii => SpectreBoxBorder.Ascii,
            BorderStyle.Double => SpectreBoxBorder.Double,
            BorderStyle.Heavy => SpectreBoxBorder.Heavy,
            BorderStyle.Rounded => SpectreBoxBorder.Rounded,
            BorderStyle.Square => SpectreBoxBorder.Square,
            BorderStyle.None => SpectreBoxBorder.None,
            _ => throw new ArgumentOutOfRangeException(nameof(style), style, null)
        };
    }

    private Cell GetCell(BoxBorderPart part)
    {
        var str = _boxBorder.GetPart(part);
        var ch = string.IsNullOrEmpty(str) ? ' ' : str[0];
        
        var character = new Character(ch);
        if (_foreground.HasValue) character = character.WithForeground(_foreground.Value);
        if (_background.HasValue) character = character.WithBackground(_background.Value);
        
        return new Cell(character);
    }

    protected override void Initialize()
    {
        lock (UI.Lock)
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

                // Available space for control (excluding scrollbar for now)
                // We reserve 1 column for scrollbar at the right of control
                var controlLimitsMin = MinSize.AsRect().Remove(totalOffset).Size;
                var controlLimitsMax = MaxSize.AsRect().Remove(totalOffset).Size;
                
                // Allow infinite height for scrolling, but constrain width to make space for scrollbar
                // If MaxSize.Width is infinite, we don't constrain width (except by MinSize/Control)
                // But we generally want to fit in MaxSize.
                
                var limitWidth = Math.Max(0, controlLimitsMax.Width - 1);
                ControlContext?.SetLimits(
                    new Size(Math.Max(0, controlLimitsMax.Width - 1), Math.Max(0, controlLimitsMax.Height)), 
                    new Size(limitWidth, int.MaxValue));

                // Clamp Top
                var viewportHeight = Math.Max(0, Size.Height - totalOffset.Top + totalOffset.Bottom); 
                // Note: Size.Height is current size. During Resize sequence, this might be stale?
                // VerticalScrollPanel uses Size.Height (which is current).
                // But here we are about to Resize.
                // If we are about to Resize to MaxSize (if control is large), then viewport will be larger.
                // Let's rely on Redraw loop?
                // Actually, we should probably use 'controlLimitsMax.Height' as the viewport constraint if we are expanding?
                // But MaxSize might be infinite.
                // Let's stick to simple clamping against current control size vs current viewport estimate?
                // Or just allow Top to be set, and Resize will clip?
                
                if (ControlContext != null)
                {
                    var controlHeight = ControlContext.Size.Height;
                    // If we expand to MaxSize, the viewport height will be at most MaxSize - Offsets.
                    var maxViewportHeight = Math.Max(0, MaxSize.Height - totalOffset.Top + totalOffset.Bottom);
                    // If MaxSize is infinite, maxViewportHeight is infinite?
                    if (MaxSize.Height == int.MaxValue) maxViewportHeight = int.MaxValue;
                    
                    // Actual viewport height used for clamping depends on what size we WILL be.
                    // But we don't know yet.
                    // However, 'Top' only matters if we are scrolling.
                    // We scroll if ControlHeight > ViewportHeight.
                    
                    _top = Math.Max(0, Math.Min(_top, controlHeight - 1)); // Ensure at least within control? 
                    // Better: _top = Math.Max(0, Math.Min(_top, controlHeight - (currentViewportHeight)));
                    // But we don't know currentViewportHeight easily before Resize.
                }
                
                ControlContext?.SetOffset(new Vector(totalOffset.Left, totalOffset.Top - _top));

                var controlSize = ControlContext?.Size ?? Size.Empty;
                
                // Calculate desired size including margins, borders, and scrollbar (1 extra width)
                var desiredControlSize = controlSize.Expand(1, 0); // +1 Width for scrollbar
                var sizeRect = desiredControlSize.AsRect().Add(totalOffset);

                Resize(Size.Clip(MinSize, sizeRect.Size, MaxSize));
                
                // Re-clamp Top after resize? 
                // If we resized, Size.Height is now updated (if Resize is immediate? No, Resize schedules/updates Size property).
                // Actually 'Control.Resize' updates 'Size' immediately in ConsoleGUI?
                // Checking ConsoleGUI source (mental): Resize usually updates Size.
                
                // Post-Resize Clamping:
                if (ControlContext != null)
                {
                     viewportHeight = Math.Max(0, Size.Height - totalOffset.Top + totalOffset.Bottom);
                     if (ControlContext.Size.Height > viewportHeight)
                     {
                         _top = Math.Min(ControlContext.Size.Height - viewportHeight, Math.Max(0, _top));
                         // Update offset again with clamped Top?
                         ControlContext.SetOffset(new Vector(totalOffset.Left, totalOffset.Top - _top));
                     }
                     else
                     {
                         _top = 0;
                         ControlContext.SetOffset(new Vector(totalOffset.Left, totalOffset.Top));
                     }
                }
            }
        }
    }

    private void BindControl()
    {
        if (Control != null)
            ControlContext = new DrawingContext(this, Control);
        else
            ControlContext = DrawingContext.Dummy;
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
    public static Offset DefaultMargin { get; } = new Offset(0, 0, 0, 0);   
    private SpectreBoxBorder _boxBorder;
    private BorderStyle _borderStyle;
    private Control _control;
    private BorderPlacement _borderPlacement = BorderPlacement.All;
    private Offset _margin;
    private Color? _foreground;
    private Color? _background;
    private DrawingContext _controlContext = DrawingContext.Dummy;
    private string? _title;
    private int _top;
    private Character _scrollBarForeground = new Character('▀', foreground: new Color(100, 100, 255));
    private Character _scrollBarBackground = new Character('║', foreground: new Color(100, 100, 100));
    #endregion

}
