namespace Jumbee.Console;

using Spectre.Console;
using Spectre.Console.Rendering;

public partial class ListBox
{
    /// <summary>
    /// An item in a ListBox.
    /// </summary>
    public class ListBoxItem
    {
        #region Constructors
        public ListBoxItem(ListBox listBox, int index, IRenderable content)
        {
            this.ListBox = listBox;
            this.Index = index;
            this._content = content;
        }

        public ListBoxItem(ListBox listBox, int index, string text, Color? foreground = null, Color? background = null)
        {
            this.ListBox = listBox;
            this.Index = index;
            this._text = text;
            this._foregroundColor = foreground;
            this._backgroundColor = background;
            UpdateTextContent();
        }
        #endregion

        #region Properties
        public readonly int Index;

        public ListBox? ListBox { get; private set; }

        private IRenderable _content = default!;
        public IRenderable Content
        {
            get => _content;
            set
            {
                _content = value;
                _text = null;
                ListBox?.Update();
            }
        }

        private string? _text;
        public string? Text
        {
            get => _text;
            set
            {
                _text = value;
                UpdateTextContent();
            }
        }

        private Color? _foregroundColor;
        public Color? ForegroundColor
        {
            get => _foregroundColor;
            set
            {
                _foregroundColor = value;
                UpdateTextContent();
            }
        }

        private Color? _backgroundColor;
        public Color? BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
                UpdateTextContent();
            }
        }
        #endregion

        #region Methods
        private void UpdateTextContent()
        {
            if (_text != null)
            {
                _content = new Markup(_text, new Spectre.Console.Style(_foregroundColor, _backgroundColor));
                ListBox?.Update();
            }
        }

        public void Detach() => ListBox = null;

        public bool IsDetached => ListBox is null;
        #endregion
    }
}