namespace Jumbee.Console;

using Spectre.Console;
using Spectre.Console.Rendering;

public partial class ListBox
{
    public class ListBoxItem
    {
        public ListBoxItem(ListBox listBox, int index, IRenderable content)
        {
            this.ListBox = listBox;
            this.Index = index;
            this.Content = content;
        }

        public readonly int Index;

        public ListBox? ListBox { get; private set; }

        private IRenderable _content = default!;
        public IRenderable Content
        {
            get => _content;
            set
            {
                _content = value;
                ListBox?.Update();
            }
        }

        public void Detach() => ListBox = null;

        public bool IsDetached => ListBox is null;
    }
}
