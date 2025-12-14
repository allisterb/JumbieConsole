using ConsoleGUI;
using ConsoleGUI.Common;
using ConsoleGUI.Data;
using ConsoleGUI.Space;
using ConsoleGUI.Utils;
using Spectre.Console.Rendering;
using System;
using SpectreBoxBorder = Spectre.Console.BoxBorder;
using SpectreBoxBorderPart = Spectre.Console.Rendering.BoxBorderPart;

namespace Jumbee.Console.Controls
{
    public enum BoxBorderPart
    {
        TopLeft = SpectreBoxBorderPart.TopLeft,
        Top = SpectreBoxBorderPart.Top,
        TopRight = SpectreBoxBorderPart.TopRight,
        Left = SpectreBoxBorderPart.Left,
        Right = SpectreBoxBorderPart.Right,
        BottomLeft = SpectreBoxBorderPart.BottomLeft,
        Bottom = SpectreBoxBorderPart.Bottom,
        BottomRight = SpectreBoxBorderPart.BottomRight
    }

    public enum BoxBorderStyle
    {
        None,
        Ascii,
        Double,
        Heavy,
        Rounded,
        Square
    }

    public class BoxBorder
    {
        internal readonly SpectreBoxBorder _border;

        private BoxBorder(SpectreBoxBorder border)
        {
            _border = border ?? throw new ArgumentNullException(nameof(border));
        }

        public string GetPart(BoxBorderPart part) => _border.GetPart((SpectreBoxBorderPart)part);

        public static implicit operator SpectreBoxBorder(BoxBorder border) => border._border;
        public static implicit operator BoxBorder(SpectreBoxBorder border) => new BoxBorder(border);

        public static readonly BoxBorder None = new BoxBorder(SpectreBoxBorder.None);
        public static readonly BoxBorder Ascii = new BoxBorder(SpectreBoxBorder.Ascii);
        public static readonly BoxBorder Double = new BoxBorder(SpectreBoxBorder.Double);
        public static readonly BoxBorder Heavy = new BoxBorder(SpectreBoxBorder.Heavy);
        public static readonly BoxBorder Rounded = new BoxBorder(SpectreBoxBorder.Rounded);
        public static readonly BoxBorder Square = new BoxBorder(SpectreBoxBorder.Square);

        public static BoxBorder FromStyle(BoxBorderStyle style)
        {
            return style switch
            {
                BoxBorderStyle.None => None,
                BoxBorderStyle.Ascii => Ascii,
                BoxBorderStyle.Double => Double,
                BoxBorderStyle.Heavy => Heavy,
                BoxBorderStyle.Rounded => Rounded,
                BoxBorderStyle.Square => Square,
                _ => throw new ArgumentOutOfRangeException(nameof(style), style, null)
            };
        }
    }

    public sealed class Border : Control, IDrawingContextListener
    {
        private DrawingContext _contentContext = DrawingContext.Dummy;
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

        private IControl? _content;
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

        private BorderPlacement _borderPlacement = BorderPlacement.All;
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

        private BoxBorder _borderStyle = BoxBorder.Square;
        public BoxBorder BorderStyle
        {
            get => _borderStyle;
            set
            {
                if (_borderStyle == value) return;
                _borderStyle = value;
                Redraw();
            }
        }

        private Color? _foreground;
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

        private Color? _background;
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
    }
}