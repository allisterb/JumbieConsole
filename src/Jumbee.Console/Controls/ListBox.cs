namespace Jumbee.Console;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Spectre.Console;
using Spectre.Console.Rendering;
using ConsoleGUI.Input;

/// <summary>
/// A list box control that displays a list of items and allows user input navigation and selection.
/// </summary>
public partial class ListBox : RenderableControl
{
    #region Constructors
    public ListBox() {}

    public ListBox(params IRenderable[] items) 
    {
        AddItems(items);
    }

    public ListBox(params string[] items )
    {
        AddItems(items);
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
    public void AddItems(params IEnumerable<IRenderable> items)
    {
        foreach (var item in items)
        {            
            var complete = false;
            while (!complete)
            {
                int index = Interlocked.Increment(ref _itemIndex);                
                complete = _items.TryAdd(index, new ListBoxItem(this, index, item));
            }                   
        }
        Invalidate();
    }

    public void AddItems(params IEnumerable<string> items)
    {
        foreach (var item in items)
        {
            var complete = false;
            while (!complete)
            {
                int index = Interlocked.Increment(ref _itemIndex);
                complete = _items.TryAdd(index, new ListBoxItem(this, index, item));
            }

        }
        Invalidate();
    }

    public void AddItems(params (string text, Color? fgColor, Color? bgColor)[] items)
    {
        foreach (var item in items)
        {
            var complete = false;
            while (!complete)
            {
                int index = Interlocked.Increment(ref _itemIndex);
                complete = _items.TryAdd(index, new ListBoxItem(this, index, item.text, item.fgColor, item.bgColor));
            }
        }
        Invalidate();
    }

    public ListBoxItem AddItem(IRenderable item)
    {
        do
        {
            int index = Interlocked.Increment(ref _itemIndex);
            var _item = new ListBoxItem(this, index, item);
            if (_items.TryAdd(index, _item)) return _item;
        }
        while (true);        
    }
  
    public ListBoxItem AddItem(string text, Color? foreground = null, Color? background = null)
    {
        do
        {
            int index = Interlocked.Increment(ref _itemIndex);
            var _item = new ListBoxItem(this, index, text, foreground, background);
            if (_items.TryAdd(index, _item)) return _item;
        }
        while (true);
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
        var items = _items.Values.OrderBy(i => i.Index).ToArray();
        
        // Ensure selection index is valid
        if (_selectionIndex >= items.Length) _selectionIndex = Math.Max(0, items.Length - 1);
        if (_selectionIndex < 0 && items.Length > 0) _selectionIndex = 0;

        var renderables = new IRenderable[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (i == _selectionIndex && item.Text != null && (_selectedForegroundColor.HasValue || _selectedBackgroundColor.HasValue))
            {
                renderables[i] = new Markup(item.Text, new Spectre.Console.Style(_selectedForegroundColor, _selectedBackgroundColor));
            }
            else
            {
                renderables[i] = item.Content;
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
