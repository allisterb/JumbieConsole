namespace Jumbee.Console;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using ConsoleGUI.Input;
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

    private Color? _selectedForegroundColor;
    public Color? SelectedForegroundColor
    {
        get => _selectedForegroundColor;
        set
        {
            _selectedForegroundColor = value;
            Invalidate();
        }
    }

    private Color? _selectedBackgroundColor;
    public Color? SelectedBackgroundColor
    {
        get => _selectedBackgroundColor;
        set
        {
            _selectedBackgroundColor = value;
            Invalidate();
        }
    }

    public override bool HandlesInput => true;
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

    public ListBoxItem AddItem(string content, Color? foreground = null, Color? background = null)
    {
        int index = Interlocked.Increment(ref _itemIndex);
        var item = new ListBoxItem(this, index, content, foreground, background);
        _items.TryAdd(index, item);
        Invalidate();
        return item;
    }

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

    public override void OnInput(InputEvent inputEvent)
    {
        if (inputEvent.Key.Key == ConsoleKey.UpArrow)
        {
            var count = _items.Count;
            if (count > 0)
            {
                _selectionIndex = (_selectionIndex - 1 + count) % count;
                Invalidate();
            }
            inputEvent.Handled = true;
        }
        else if (inputEvent.Key.Key == ConsoleKey.DownArrow)
        {
            var count = _items.Count;
            if (count > 0)
            {
                _selectionIndex = (_selectionIndex + 1) % count;
                Invalidate();
            }
            inputEvent.Handled = true;
        }
    }

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var items = _items.Values.OrderBy(i => i.Index).ToList();
        
        // Ensure selection index is valid
        if (_selectionIndex >= items.Count) _selectionIndex = Math.Max(0, items.Count - 1);
        if (_selectionIndex < 0 && items.Count > 0) _selectionIndex = 0;

        var renderables = new List<IRenderable>();

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (i == _selectionIndex && item.Text != null && (_selectedForegroundColor.HasValue || _selectedBackgroundColor.HasValue))
            {
                renderables.Add(new Markup(item.Text, new Spectre.Console.Style(_selectedForegroundColor, _selectedBackgroundColor)));
            }
            else
            {
                renderables.Add(item.Content);
            }
        }

        var rows = new Rows(renderables);
        return ((IRenderable)rows).Render(options, maxWidth);
    }
    #endregion

    #region Fields
    private readonly ConcurrentDictionary<int, ListBoxItem> _items = new();
    private int _itemIndex = -1;
    private int _selectionIndex = 0;
    #endregion
}
