namespace Jumbee.Console;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

using Spectre.Console;
using Spectre.Console.Rendering;
/// <summary>
/// A bar chart. Based on Spectre.Console.BarChart
/// </summary>
public class BarChart : RenderableControl, Spectre.Console.IHasCulture
{
    
    public void Update() => Invalidate();

    /// <summary>
    /// Gets the bar chart data.
    /// </summary>

    public ChartOrientation Orientation
    {
        get => field;
        set
        {
            field = value;
            _renderables.Clear();
            Invalidate();
        }
    }
    public ICollection<BarChartItem> Data => data.Values;

    /// <summary>
    /// Gets or sets the bar chart label.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the bar chart label alignment.
    /// </summary>
    public Justify? LabelAlignment { get; set; } = Justify.Center;

    /// <summary>
    /// Gets or sets a value indicating whether or not
    /// values should be shown next to each bar.
    /// </summary>
    public bool ShowValues { get; set; } = true;

    /// <summary>
    /// Gets or sets the culture that's used to format values.
    /// </summary>
    /// <remarks>Defaults to invariant culture.</remarks>
    public CultureInfo? Culture { get; set; }

    /// <summary>
    /// Gets or sets the fixed max value for a bar chart.
    /// </summary>
    /// <remarks>Defaults to null, which corresponds to largest value in chart.</remarks>
    public double? MaxValue { get; set; }

    /// <summary>
    /// Gets or sets the function used to format the values of the bar chart.
    /// </summary>
    public Func<double, CultureInfo, string>? ValueFormatter { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BarChart"/> class.
    /// </summary>
    public BarChart(params (string label, double value, Color color)[] items) : this(ChartOrientation.Horizontal, items)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BarChart"/> class.
    /// </summary>
    public BarChart(ChartOrientation orientation, params (string label, double value, Color color)[] items)
    {
        Orientation = orientation;
        foreach (var item in items)
        {
            while (!data.TryAdd(Interlocked.Increment(ref itemIndex), new BarChartItem(this, item.label, item.value, item.color))) ;
        }
        Invalidate();
    }

    public int? BarWidth
    {
        get => Width;
        set
        {
            if (value.HasValue)
            {
                Width = value.Value;
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
            }
        }
    }

    public double[] this[params string[] labels]
    {
        set
        {
            if (labels.Length != value.Length)
            {
                throw new ArgumentException("Labels and values count mismatch");
            }

            foreach (var kvp in data)
            {
                var idx = Array.IndexOf(labels, kvp.Value.Label);
                if (idx >= 0)
                {
                    var item = kvp.Value;
                    item.Value = value[idx];
                    data[kvp.Key] = item;
                }
            }
            Invalidate();
        }
    }

    public BarChartItem AddItem(string label, double value, Color color)
    {

        int index = -1;
        while (!data.TryAdd(index = Interlocked.Increment(ref itemIndex), new BarChartItem(this, label, value, color)));
        Invalidate();
        return data[index];
    }

    public BarChart AddItems(params (string label, double value, Color color)[] items)
    {
        foreach (var item in items)
        {
            while (!data.TryAdd(Interlocked.Increment(ref itemIndex), new BarChartItem(this, item.label, item.value, item.color))) ;
        }
        Invalidate();
        return this;
    }

    public bool RemoveItem(int index)
    {
        if (data.TryRemove(index, out var item))
        {
            _renderables.Remove(index);
            item.Detach();
            Update();
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <inheritdoc/>
    protected override Measurement Measure(RenderOptions options, int maxWidth)
    {
        var width = Math.Min(Width, maxWidth);
        return new Measurement(width, width);
    }

    protected int itemIndex = -1;
    protected char VerticalUnicodeBar { get; set; } = '━';
    protected char AsciiBar { get; set; } = '-';
    protected static char HorizontalUnicodeBar { get; set; } = '█';
    protected readonly ConcurrentDictionary<int, BarChartItem> data = new();

    /// <inheritdoc/>
    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var width = Math.Min(Width, maxWidth);
        var maxValue = Math.Max(MaxValue ?? 0d, data.Values.Select(item => item.Value).DefaultIfEmpty(0).Max());

        if (Orientation == ChartOrientation.Vertical)
        {
            var grid = new Spectre.Console.Grid();
            grid.Collapse();
            foreach (var _ in Data)
            {
                grid.AddColumn(new GridColumn().Centered());
            }
            grid.Width = width;

            int itemLabelHeight = 1; 
            int itemValueHeight = ShowValues ? 1 : 0;
            
            int effectiveHeight = Height > 0 ? Height : (10 + itemLabelHeight + itemValueHeight + (string.IsNullOrWhiteSpace(Label) ? 0 : 1));
            int barHeight = Math.Max(1, effectiveHeight - itemLabelHeight - itemValueHeight - (string.IsNullOrWhiteSpace(Label) ? 0 : 1));

            var bars = new List<IRenderable>();
            var values = new List<IRenderable>();
            var labels = new List<IRenderable>();

            foreach (var kvp in data)
            {
                var id = kvp.Key;
                var item = kvp.Value;

                if (!_renderables.TryGetValue(id, out var renderable) || !(renderable is VerticalBar))
                {
                    renderable = new VerticalBar();
                    _renderables[id] = renderable;
                }

                var bar = (VerticalBar)renderable;
                bar.Value = item.Value;
                bar.MaxValue = maxValue;
                bar.Height = barHeight;
                bar.Color = item.Color;
                bar.UnicodeBar = VerticalUnicodeBar;
                bar.AsciiBar = AsciiBar;

                bars.Add(bar);

                if (ShowValues)
                {
                    var val = ValueFormatter != null 
                        ? ValueFormatter(item.Value, Culture ?? CultureInfo.InvariantCulture) 
                        : item.Value.ToString(Culture ?? CultureInfo.InvariantCulture);
                    values.Add(new Markup(val, new Spectre.Console.Style(foreground: item.Color)));
                }

                labels.Add(new Markup(item.Label, new Spectre.Console.Style(foreground: item.Color)));
            }

            grid.AddRow(bars.ToArray());

            if (ShowValues)
            {
                grid.AddRow(values.ToArray());
            }

            grid.AddRow(labels.ToArray());

            if (!string.IsNullOrWhiteSpace(Label))
            {
                 var container = new Spectre.Console.Grid();
                 container.Collapse();
                 container.AddColumn(new GridColumn().Centered());
                 container.Width = width;
                 container.AddRow(new Markup(Label).Justify(LabelAlignment.HasValue ? (Spectre.Console.Justify)LabelAlignment.Value : Spectre.Console.Justify.Center));
                 container.AddRow(grid);
                 return ((IRenderable)container).Render(options, width);
            }

            return ((IRenderable)grid).Render(options, width);
        }
        else
        {
            var horizontalGrid = new Spectre.Console.Grid();
            horizontalGrid.Collapse();
            horizontalGrid.AddColumn(new GridColumn().PadRight(2).RightAligned());
            horizontalGrid.AddColumn(new GridColumn().PadLeft(0));
            horizontalGrid.Width = width;

            if (!string.IsNullOrWhiteSpace(Label))
            {
                horizontalGrid.AddRow(Text.Empty, new Markup(Label).Justify((Spectre.Console.Justify?)LabelAlignment));
            }

            foreach (var kvp in data)
            {
                var id = kvp.Key;
                var item = kvp.Value;

                if (!_renderables.TryGetValue(id, out var renderable) || !(renderable is ProgressBar))
                {
                    renderable = new ProgressBar();
                    _renderables[id] = renderable;
                }

                var bar = (ProgressBar)renderable;
                bar.Value = item.Value;
                bar.MaxValue = maxValue;
                bar.ShowRemaining = false;
                bar.CompletedStyle = item.Color;
                bar.FinishedStyle = item.Color;
                bar.UnicodeBar = HorizontalUnicodeBar;
                bar.AsciiBar = AsciiBar;
                bar.ShowValue = ShowValues;
                bar.Culture = Culture;
                bar.ValueFormatter = ValueFormatter;

                horizontalGrid.AddRow(
                    new Markup(item.Label),
                    bar);
            }
            
            return ((IRenderable)horizontalGrid).Render(options, width);
        }
    }


    private readonly Dictionary<int, Renderable> _renderables = new();

    private sealed class VerticalBar : Renderable
    {
        public double Value { get; set; }
        public double MaxValue { get; set; }
        public int Height { get; set; }
        public Color Color { get; set; }
        public char UnicodeBar { get; set; } = '█';
        public char AsciiBar { get; set; } = '|';

        protected override Measurement Measure(RenderOptions options, int maxWidth)
        {
            return new Measurement(1, maxWidth); 
        }

        protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            var barChar = !options.Unicode ? AsciiBar : UnicodeBar;
            var ratio = MaxValue > 0 ? Math.Clamp(Value / MaxValue, 0, 1) : 0;
            var barHeight = (int)Math.Round(ratio * Height);
            var emptyHeight = Height - barHeight;

            var style = new Spectre.Console.Style(foreground: Color);

            for (int i = 0; i < emptyHeight; i++)
            {
                yield return new Segment(new string(' ', 1) + "\n"); // Or just " \n"
                // Actually Segment usually doesn't contain newline for Grid cells? 
                // Grid handles newlines. If we return multiple segments, they are just concatenated?
                // No, IRenderable.Render usually returns a flow of segments. 
                // In a Grid cell, if we want multiple lines, we must emit newlines.
                // However, Spectre.Console Grid cells can wrap or handle explicit newlines.
                // Let's try explicit newlines.
            }
            
            for (int i = 0; i < barHeight; i++)
            {
                // We render the bar character. 
                // We should probably repeat it for width? 
                // But VerticalBar is usually 1 char wide or full cell width?
                // Let's assume full cell width. But we don't know the exact width assigned by Grid here easily 
                // unless we use maxWidth.
                // For a simple vertical bar, let's use 3 chars wide? Or just 1? 
                // Let's use maxWidth to fill the column.
                // But Grid column width is determined by content... circular dependency?
                // No, Grid passes maxWidth.
                // Let's default to a fixed width if maxWidth is huge (which it is in Grid auto-sizing).
                // Let's say 3 chars wide.
                var w = Math.Min(3, maxWidth);
                var text = new string(barChar, w);
                
                // If it's not the last line, add newline
                // Actually, for the empty lines above, we also need width.
            }
             
            // Re-thinking render strategy:
            // We want to return a block of text.
            // Empty lines first, then filled lines.
            
            var w2 = Math.Min(3, maxWidth); // Fixed width of 3 for now
            
            for (int i = 0; i < emptyHeight; i++)
            {
                 yield return new Segment(new string(' ', w2));
                 yield return Segment.LineBreak;
            }
             
            for (int i = 0; i < barHeight; i++)
            {
                 yield return new Segment(new string(barChar, w2), style);
                 if (i < barHeight - 1) yield return Segment.LineBreak;
            }
        }
    }

    private sealed class ProgressBar : Renderable
    {
        public double Value { get; set; }
        public double MaxValue { get; set; } = 100;

        public int? Width { get; set; }
        public bool ShowRemaining { get; set; } = true;
        public char UnicodeBar { get; set; } = '━';
        public char AsciiBar { get; set; } = '-';
        public bool ShowValue { get; set; }
        public CultureInfo? Culture { get; set; }
        public Func<double, CultureInfo, string>? ValueFormatter { get; set; }

        public Style CompletedStyle { get; set; } = Color.Yellow;
        public Style FinishedStyle { get; set; } = Color.Green;
        public Style RemainingStyle { get; set; } = Color.Grey;

        protected override Measurement Measure(RenderOptions options, int maxWidth)
        {
            var width = Math.Min(Width ?? maxWidth, maxWidth);
            return new Measurement(4, width);
        }

        protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            var width = Math.Min(Width ?? maxWidth, maxWidth);
            var completedBarCount = Math.Min(MaxValue, Math.Max(0, Value));
            var isCompleted = completedBarCount >= MaxValue;

            var bar = !options.Unicode ? AsciiBar : UnicodeBar;
            var style = isCompleted ? FinishedStyle : CompletedStyle;
            var barCount = Math.Max(0, (int)(width * (completedBarCount / MaxValue)));

            // Show value?
            var value = ValueFormatter != null ? ValueFormatter(completedBarCount, Culture ?? CultureInfo.InvariantCulture) : completedBarCount.ToString(Culture ?? CultureInfo.InvariantCulture);
            if (ShowValue)
            {
                barCount = barCount - value.Length - 1;
                barCount = Math.Max(0, barCount);
            }

            yield return new Segment(new string(bar, barCount), style);

            if (ShowValue)
            {
                yield return barCount == 0
                    ? new Segment(value, style)
                    : new Segment(" " + value, style);
            }

            // More space available?
            if (barCount < width)
            {
                var diff = width - barCount;
                if (ShowValue)
                {
                    diff = diff - value.Length - 1;
                    if (diff <= 0)
                    {
                        yield break;
                    }
                }

                var legacy = options.ColorSystem == ColorSystem.NoColors || options.ColorSystem == ColorSystem.Legacy;
                var remainingToken = ShowRemaining && !legacy ? bar : ' ';
                yield return new Segment(new string(remainingToken, diff), RemainingStyle);
            }
        }
    }}
