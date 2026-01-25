namespace Jumbee.Console;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Spectre.Console;
using Spectre.Console.Rendering;

/// <summary>
/// A list box control that displays a list of items.
/// </summary>
public partial class ListBox : RenderableControl
{
    #region Constructors
    public ListBox()
    {
    }

    public ListBox(IEnumerable<IRenderable> items)
    {
        foreach (var item in items)
        {
            AddItem(item);
        }
    }

    public ListBox(IEnumerable<string> items)
    {
        foreach (var item in items)
        {
            AddItem(item);
        }
    }
    #endregion

    #region Properties
    public ICollection<ListBoxItem> Items => _items.Values;
    #endregion

    #region Methods
    public ListBoxItem AddItem(IRenderable content)
    {
        int index = Interlocked.Increment(ref _itemIndex);
        var item = new ListBoxItem(this, index, content);
        _items.TryAdd(index, item);
        Invalidate();
        return item;
    }

    public ListBoxItem AddItem(string content) => AddItem(new Markup(content));

    public void AddItems(IEnumerable<IRenderable> items)
    {
        foreach (var item in items)
        {
            AddItem(item);
        }
    }

    public void AddItems(IEnumerable<string> items)
    {
        foreach (var item in items)
        {
            AddItem(item);
        }
    }

    public bool RemoveItem(ListBoxItem item)
    {
        if (_items.TryRemove(item.Index, out var removed))
        {
            removed.Detach();
            Invalidate();
            return true;
        }
        return false;
    }

    public void Clear()
    {
        foreach (var item in _items.Values)
        {
            item.Detach();
        }
        _items.Clear();
        Invalidate();
    }

    public void Update() => Invalidate();

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var rows = new Rows(_items.Values.OrderBy(i => i.Index).Select(i => i.Content));
        return ((IRenderable)rows).Render(options, maxWidth);
    }
    #endregion

    #region Fields
    private readonly ConcurrentDictionary<int, ListBoxItem> _items = new();
    private int _itemIndex = -1;
    #endregion
}
