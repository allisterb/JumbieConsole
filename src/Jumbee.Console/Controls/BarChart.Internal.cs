namespace Jumbee.Console;

using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public partial class BarChart
{
    public class BarChartItem
    {
        public BarChartItem(BarChart chart, int index, string label, double value, Color color)
        {
            this.Index = index;
            this.Label = label;
            this.Value = value;
            this.Color = color;
            this.chart = chart;
        }

        public readonly int Index;

        private BarChart? chart;

        public BarChart? Chart => chart;

        /// <summary>
        /// Gets the item label.
        /// </summary>
        public string Label
        {
            get => field;
            set
            {
                field = value;
            }
        }

        /// <summary>
        /// Gets the item value.
        /// </summary>
        public double Value
        {
            get => field;
            set
            {
                field = value;
                chart?.UpdateItemValue(Index, value);
            }
        }

        /// <summary>
        /// Gets the item color.
        /// </summary>
        public Color Color
        {
            get => field;
            set
            {
                field = value;
                chart?.UpdateItemColor(Index, value);
            }
        }

        public void Detach() => chart = null;

        public bool IsDetached => chart is null;

        public void UpdateChart() => chart?.Update();


    }

    protected interface IBarControl : IRenderable
    {
        double Value { get; set; }
        double MaxValue { get; set; }
        Color Color { get; set; }
    }

    protected class VerticalBar : Renderable, IBarControl
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

    protected sealed class HorizontalBar : Renderable, IBarControl
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

        public Color Color { get => CompletedStyle; set { CompletedStyle = value; FinishedStyle = value; } }

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
    }
}
