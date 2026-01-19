namespace Jumbee.Console;
﻿
﻿using Spectre.Console;
﻿using Spectre.Console.Rendering;
﻿using System;
﻿using System.Collections.Concurrent;
﻿using System.Collections.Generic;
﻿using System.Globalization;
﻿using System.Linq;
﻿using System.Threading;
﻿
﻿/// <summary>
﻿/// A bar chart. Based on Spectre.Console.BarChart
﻿/// </summary>
﻿public class BarChart : RenderableControl, Spectre.Console.IHasCulture
﻿{
﻿    public void Update() => Invalidate();
﻿
    /// <summary>
    /// Gets the bar chart data.
    /// </summary>
    public ChartOrientation Orientation
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                CreateChartElements();
                Invalidate();
            }
        }
    }

    public ICollection<BarChartItem> Data => data.Values;

    /// <summary>
    /// Gets or sets the bar chart label.
    /// </summary>
    public string? Label 
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                CreateChartLabel();
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the bar chart label alignment.
    /// </summary>
    public Justify? LabelAlignment 
    {
        get => field;
        set 
        {
            if (field != value)
            {
                field = value;
                CreateChartLabel();
                Invalidate();
            }
        }
    } 

    private bool _showValues = true;
    public bool ShowValues
    {
        get => _showValues;
        set
        {            
            if (_showValues != value)
            {
                _showValues = value;
                CreateChartElements();
                Invalidate();
            }
        }
    }

    public CultureInfo? Culture { get; set; }

    private double? _maxValue;
    public double? MaxValue
    {
        get => _maxValue;
        set
        {
            if (_maxValue != value)
            {
                _maxValue = value;
                UpdateAllBars();
                Invalidate();
            }
        }
    }

    public Func<double, CultureInfo, string>? ValueFormatter { get; set; }

    public BarChart(params (string label, double value, Color color)[] items) : this(ChartOrientation.Horizontal, items)
    {
    }

    public BarChart(ChartOrientation orientation, params (string label, double value, Color color)[] items)
    {
        Orientation = orientation;
        int index;
        foreach (var item in items)
        {
            index = Interlocked.Increment(ref itemIndex);
            while (!data.TryAdd(index, new BarChartItem(this, index, item.label, item.value, item.color))) ;
        }
        CreateChartElements();
        Invalidate();
    }

    public int? BarWidth
    {
        get => Width;
        set
        {
            if (value.HasValue && Width != value.Value)
            {
                Width = value.Value;
                UpdateAllBars();
                Invalidate();
            }
        }
    }

    public bool CenterLabel
    {
        set
        {
            if (value)
            {
                LabelAlignment = Justify.Center;
                CreateChartLabel(); // Update label alignment
                Invalidate();
            }
        }
    }
﻿
﻿    public double[] this[params string[] labels]
﻿    {
﻿        set
﻿        {
﻿            if (labels.Length != value.Length)
﻿            {
﻿                throw new ArgumentException("Labels and values count mismatch");
﻿            }
﻿
﻿            foreach (var kvp in data)
﻿            {
﻿                var idx = Array.IndexOf(labels, kvp.Value.Label);
﻿                if (idx >= 0)
﻿                {
﻿                    // This setter triggers UpdateItemValue via BarChartItem
﻿                    var item = kvp.Value;
﻿                    item.Value = value[idx];
﻿                    data[kvp.Key] = item;
﻿                }
﻿            }
﻿        }
﻿    }
﻿
﻿    public BarChartItem AddItem(string label, double value, Color color)
﻿    {
﻿        int index = -1;
﻿        bool completed = false;
﻿        while (!completed)
﻿        {
﻿            index = Interlocked.Increment(ref itemIndex);
﻿            completed = data.TryAdd(index, new BarChartItem(this, index, label, value, color));
﻿        }
﻿        CreateChartElements();
﻿        Invalidate();
﻿        return data[index];
﻿    }
﻿
﻿    public BarChart AddItems(params (string label, double value, Color color)[] items)
﻿    {
﻿        foreach (var item in items)
﻿        {
﻿            int index = -1;
﻿            bool completed = false;
﻿            while (!completed)
﻿            {
﻿                index = Interlocked.Increment(ref itemIndex);
﻿                completed = data.TryAdd(index, new BarChartItem(this, index, item.label, item.value, item.color));
﻿            }
﻿        }
﻿        CreateChartElements();
﻿        Invalidate();
﻿        return this;
﻿    }
﻿
﻿    public bool RemoveItem(int index)
﻿    {
﻿        if (data.TryRemove(index, out var item))
﻿        {
﻿            CreateChartElements();
﻿            item.Detach();
﻿            Invalidate();
﻿            return true;
﻿        }
﻿        else
﻿        {
﻿            return false;
﻿        }
﻿    }
﻿
﻿    public bool RemoveItem(BarChartItem item) => RemoveItem(item.Index);
﻿
﻿    protected override Measurement Measure(RenderOptions options, int maxWidth)
﻿    {
﻿        var width = Math.Min(Width, maxWidth);
﻿        return new Measurement(width, width);
﻿    }
﻿
﻿    protected int itemIndex = -1;
﻿    protected char VerticalUnicodeBar { get; set; } = '━';
﻿    protected char AsciiBar { get; set; } = '-';
﻿    protected static char HorizontalUnicodeBar { get; set; } = '█';
﻿    protected readonly ConcurrentDictionary<int, BarChartItem> data = new();
﻿
﻿    protected Spectre.Console.Grid _grid = new();
    protected Spectre.Console.Grid? _containerGrid = new();
    protected List<IBarControl> _bars = new();
﻿    protected List<IRenderable> _labels = new();
﻿    protected List<IRenderable> _values = new();
﻿
﻿    private int GetListIndex(int id)
﻿    {
﻿        // Data keys are sorted in CreateChartElements to populate the lists.
﻿        // We need to match that order.
﻿        var sortedKeys = data.Keys.OrderBy(k => k).ToList();
﻿        return sortedKeys.IndexOf(id);
﻿    }
﻿
﻿    internal void UpdateItemValue(int id, double value)
﻿    {
﻿        int i = GetListIndex(id);
﻿        if (i >= 0 && i < _bars.Count)
﻿        {
﻿            var bar = _bars[i];
﻿           
﻿            
﻿            if (bar is VerticalBar vb)
﻿            {
﻿                vb.Value = value;
                // Recalc height
﻿                int itemLabelHeight = 1;
﻿                int itemValueHeight = ShowValues ? 1 : 0;
﻿                int effectiveHeight = Height > 0 ? Height : (10 + itemLabelHeight + itemValueHeight + (string.IsNullOrWhiteSpace(Label) ? 0 : 1));
﻿                vb.Height = Math.Max(1, effectiveHeight - itemLabelHeight - itemValueHeight - (string.IsNullOrWhiteSpace(Label) ? 0 : 1));
﻿            }

            else if (bar is HorizontalBar pb)
            {
                pb.Value = value;
                // Recalc height
                int itemLabelWidth = 1;
                int itemValueWidth = ShowValues ? 1 : 0;
                int effectiveWidth = Width > 0 ? Width : (10 + itemLabelWidth + itemValueWidth);
                pb.Width = Math.Max(1, effectiveWidth - itemLabelWidth - itemValueWidth);
            }

            if (ShowValues && i < _values.Count)
            {
                var valStr = ValueFormatter != null
                            ? ValueFormatter(value, Culture ?? CultureInfo.InvariantCulture)
                            : value.ToString(Culture ?? CultureInfo.InvariantCulture);
                _values[i] = new Markup(valStr);
            }
﻿            Invalidate();
﻿        }
﻿    }
﻿
﻿    internal void UpdateItemLabel(int id, string label)
﻿    {
﻿        int i = GetListIndex(id);
﻿        if (i >= 0 && i < _labels.Count)
﻿        {
﻿            _labels[i] = new Markup(label);
﻿            Invalidate();
﻿        }
﻿    }
﻿
﻿    internal void UpdateItemColor(int id, Color color)
﻿    {
﻿        int i = GetListIndex(id);
﻿        if (i >= 0)
﻿        {
﻿            if (i < _bars.Count) _bars[i].Color = color;
﻿            if (i < _labels.Count) _labels[i] = new Markup(data[i].Label, new Spectre.Console.Style(foreground: color));
﻿            if (ShowValues && i < _values.Count) _values[i] = new Markup(data[i].Label, new Spectre.Console.Style(foreground: color));
﻿            Invalidate();
﻿        }
﻿    }
﻿
﻿    protected void UpdateAllBars()
﻿    {
﻿        var maxValue = Math.Max(MaxValue ?? 0d, data.Values.Select(item => item.Value).DefaultIfEmpty(0).Max());
﻿        foreach (var bar in _bars)
﻿        {
﻿            bar.MaxValue = maxValue;
﻿            if (bar is HorizontalBar pb)
﻿            {
﻿                pb.Width = Width;
﻿            }
﻿            else if (bar is VerticalBar vb)
﻿            {
﻿                int itemLabelHeight = 1;
﻿                int itemValueHeight = ShowValues ? 1 : 0;
﻿                int effectiveHeight = Height > 0 ? Height : (10 + itemLabelHeight + itemValueHeight + (string.IsNullOrWhiteSpace(Label) ? 0 : 1));
﻿                vb.Height = Math.Max(1, effectiveHeight - itemLabelHeight - itemValueHeight - (string.IsNullOrWhiteSpace(Label) ? 0 : 1));
﻿            }
﻿        }
﻿    }
﻿
    protected void CreateChartLabel()
    {
        if (string.IsNullOrWhiteSpace(Label))
        {
            _containerGrid = null;
        }
        else
        {
            _containerGrid = new Spectre.Console.Grid();
            _containerGrid.Collapse();
            _containerGrid.AddColumn(new GridColumn().Centered());
            
            _containerGrid.AddRow(new Markup(Label).Justify(LabelAlignment.HasValue ? (Spectre.Console.Justify)LabelAlignment.Value : Spectre.Console.Justify.Center));
            _containerGrid.AddRow(_grid);
        }
    }

    protected void CreateChartElements()
    {
        _grid = new Spectre.Console.Grid();
        _grid.Collapse();
        _bars.Clear();
        _labels.Clear();
        _values.Clear();

        var sortedData = data.Values.OrderBy(x => x.Index).ToList();
        var maxValue = Math.Max(MaxValue ?? 0d, sortedData.Select(item => item.Value).DefaultIfEmpty(0).Max());

        if (Orientation == ChartOrientation.Vertical)
        {
            foreach (var _ in sortedData)
            {
                _grid.AddColumn(new GridColumn().Centered());
            }

            int itemLabelHeight = 1; 
            int itemValueHeight = ShowValues ? 1 : 0;
            int effectiveHeight = Height > 0 ? Height : (10 + itemLabelHeight + itemValueHeight + (string.IsNullOrWhiteSpace(Label) ? 0 : 1));
            int barHeight = Math.Max(1, effectiveHeight - itemLabelHeight - itemValueHeight - (string.IsNullOrWhiteSpace(Label) ? 0 : 1));

            var barRenderables = new List<IRenderable>();
            foreach (var item in sortedData)
            {
                var bar = new VerticalBar
                {
                    Value = item.Value,
                    MaxValue = maxValue,
                    Height = barHeight,
                    Color = item.Color,
                    UnicodeBar = VerticalUnicodeBar,
                    AsciiBar = AsciiBar
                };
                _bars.Add(bar);
                barRenderables.Add(bar);
            }
            _grid.AddRow(barRenderables.ToArray());

            if (ShowValues)
            {
                var valueRenderables = new List<IRenderable>();
                foreach (var item in sortedData)
                {
                    var valStr = ValueFormatter != null 
                        ? ValueFormatter(item.Value, Culture ?? CultureInfo.InvariantCulture) 
                        : item.Value.ToString(Culture ?? CultureInfo.InvariantCulture);
                    var mk = new Markup(valStr, new Spectre.Console.Style(foreground: item.Color));
                    _values.Add(mk);
                    valueRenderables.Add(mk);
                }
                _grid.AddRow(valueRenderables.ToArray());
            }

            var labelRenderables = new List<IRenderable>();
            foreach (var item in sortedData)
            {
                 var mk = new Markup(item.Label, new Spectre.Console.Style(foreground: item.Color));
                 _labels.Add(mk);
                 labelRenderables.Add(mk);
            }
            _grid.AddRow(labelRenderables.ToArray());
        }
        else // Horizontal
        {
            _grid.AddColumn(new GridColumn().PadRight(2).RightAligned());
            _grid.AddColumn(new GridColumn().PadLeft(0));

            foreach (var item in sortedData)
            {
                var mkLabel = new Markup(item.Label, new Spectre.Console.Style(foreground: item.Color));
                _labels.Add(mkLabel);

                var bar = new HorizontalBar
                {
                    Value = item.Value,
                    MaxValue = maxValue,
                    ShowRemaining = false,
                    Color = item.Color,
                    UnicodeBar = HorizontalUnicodeBar,
                    AsciiBar = AsciiBar,
                    ShowValue = ShowValues,
                    Culture = Culture,
                    ValueFormatter = ValueFormatter
                };
                _bars.Add(bar);

                _grid.AddRow(mkLabel, bar);
            }
        }
        CreateChartLabel();
    }

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var width = Math.Min(Width, maxWidth);
        var grid = _containerGrid ?? _grid;        
        grid.Width = width;       
        return ((IRenderable)grid).Render(options, width);
    }

    public interface IBarControl : IRenderable
﻿    {
﻿        double Value { get; set; }
﻿        double MaxValue { get; set; }
﻿        Color Color { get; set; }
﻿    }
﻿﻿   ﻿
﻿    internal sealed class VerticalBar : Renderable, IBarControl
﻿    {
﻿        public double Value { get; set; }
﻿        public double MaxValue { get; set; }
﻿        public int Height { get; set; }
﻿        public Color Color { get; set; }
﻿        public char UnicodeBar { get; set; } = '█';
﻿        public char AsciiBar { get; set; } = '|';
﻿
﻿        protected override Measurement Measure(RenderOptions options, int maxWidth)
﻿        {
﻿            return new Measurement(1, maxWidth); 
﻿        }
﻿
﻿        protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
﻿        {
﻿            var barChar = !options.Unicode ? AsciiBar : UnicodeBar;
﻿            var ratio = MaxValue > 0 ? Math.Clamp(Value / MaxValue, 0, 1) : 0;
﻿            var barHeight = (int)Math.Round(ratio * Height);
﻿            var emptyHeight = Height - barHeight;
﻿
﻿            var style = new Spectre.Console.Style(foreground: Color);
﻿
﻿            for (int i = 0; i < emptyHeight; i++)
﻿            {
﻿                yield return new Segment(new string(' ', 1) + "\n"); // Or just " \n"
﻿                // Actually Segment usually doesn't contain newline for Grid cells? 
﻿                // Grid handles newlines. If we return multiple segments, they are just concatenated?
﻿                // No, IRenderable.Render usually returns a flow of segments. 
﻿                // In a Grid cell, if we want multiple lines, we must emit newlines.
﻿                // However, Spectre.Console Grid cells can wrap or handle explicit newlines.
﻿                // Let's try explicit newlines.
﻿            }
﻿            
﻿            for (int i = 0; i < barHeight; i++)
﻿            {
﻿                // We render the bar character. 
﻿                // We should probably repeat it for width? 
﻿                // But VerticalBar is usually 1 char wide or full cell width?
﻿                // Let's assume full cell width. But we don't know the exact width assigned by Grid here easily 
﻿                // unless we use maxWidth.
﻿                // For a simple vertical bar, let's use 3 chars wide? Or just 1? 
﻿                // Let's use maxWidth to fill the column.
﻿                // But Grid column width is determined by content... circular dependency?
﻿                // No, Grid passes maxWidth.
﻿                // Let's default to a fixed width if maxWidth is huge (which it is in Grid auto-sizing).
﻿                // Let's say 3 chars wide.
﻿                var w = Math.Min(3, maxWidth);
﻿                var text = new string(barChar, w);
﻿                
﻿                // If it's not the last line, add newline
﻿                // Actually, for the empty lines above, we also need width.
﻿            }
﻿             
﻿            // Re-thinking render strategy:
﻿            // We want to return a block of text.
﻿            // Empty lines first, then filled lines.
﻿            
﻿            var w2 = Math.Min(3, maxWidth); // Fixed width of 3 for now
﻿            
﻿            for (int i = 0; i < emptyHeight; i++)
﻿            {
﻿                 yield return new Segment(new string(' ', w2));
﻿                 yield return Segment.LineBreak;
﻿            }
﻿             
﻿            for (int i = 0; i < barHeight; i++)
﻿            {
﻿                 yield return new Segment(new string(barChar, w2), style);
﻿                 if (i < barHeight - 1) yield return Segment.LineBreak;
﻿            }
﻿        }
﻿    }
﻿
﻿    internal sealed class HorizontalBar : Renderable, IBarControl
﻿    {
﻿        public double Value { get; set; }
﻿        public double MaxValue { get; set; } = 100;
﻿
﻿        public int? Width { get; set; }
﻿        public bool ShowRemaining { get; set; } = true;
﻿        public char UnicodeBar { get; set; } = '━';
﻿        public char AsciiBar { get; set; } = '-';
﻿        public bool ShowValue { get; set; }
﻿        public CultureInfo? Culture { get; set; }
﻿        public Func<double, CultureInfo, string>? ValueFormatter { get; set; }
﻿
﻿        public Color Color { get => CompletedStyle; set { CompletedStyle = value; FinishedStyle = value; } }
﻿
﻿        public Style CompletedStyle { get; set; } = Color.Yellow;
﻿        public Style FinishedStyle { get; set; } = Color.Green;
﻿        public Style RemainingStyle { get; set; } = Color.Grey;
﻿
﻿        protected override Measurement Measure(RenderOptions options, int maxWidth)
﻿        {
﻿            var width = Math.Min(Width ?? maxWidth, maxWidth);
﻿            return new Measurement(4, width);
﻿        }
﻿
﻿        protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
﻿        {
﻿            var width = Math.Min(Width ?? maxWidth, maxWidth);
﻿            var completedBarCount = Math.Min(MaxValue, Math.Max(0, Value));
﻿            var isCompleted = completedBarCount >= MaxValue;
﻿
﻿            var bar = !options.Unicode ? AsciiBar : UnicodeBar;
﻿            var style = isCompleted ? FinishedStyle : CompletedStyle;
﻿            var barCount = Math.Max(0, (int)(width * (completedBarCount / MaxValue)));
﻿
﻿            // Show value?
﻿            var value = ValueFormatter != null ? ValueFormatter(completedBarCount, Culture ?? CultureInfo.InvariantCulture) : completedBarCount.ToString(Culture ?? CultureInfo.InvariantCulture);
﻿            if (ShowValue)
﻿            {
﻿                barCount = barCount - value.Length - 1;
﻿                barCount = Math.Max(0, barCount);
﻿            }
﻿
﻿            yield return new Segment(new string(bar, barCount), style);
﻿
﻿            if (ShowValue)
﻿            {
﻿                yield return barCount == 0
﻿                    ? new Segment(value, style)
﻿                    : new Segment(" " + value, style);
﻿            }
﻿
﻿            // More space available?
﻿            if (barCount < width)
﻿            {
﻿                var diff = width - barCount;
﻿                if (ShowValue)
﻿                {
﻿                    diff = diff - value.Length - 1;
﻿                    if (diff <= 0)
﻿                    {
﻿                        yield break;
﻿                    }
﻿                }
﻿
﻿                var legacy = options.ColorSystem == ColorSystem.NoColors || options.ColorSystem == ColorSystem.Legacy;
﻿                var remainingToken = ShowRemaining && !legacy ? bar : ' ';
﻿                yield return new Segment(new string(remainingToken, diff), RemainingStyle);
﻿            }
﻿        }
﻿    }
﻿}
