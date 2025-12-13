namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using Spectre.Console;

public class BarChart : SpectreControl<Spectre.Console.BarChart>
{
    public BarChart() : base(new Spectre.Console.BarChart()) {}

    public BarChart(params (string Label, double Value, Color? Color)[] items) : this()
    {
        foreach (var (label, value, color) in items)
        {
            AddItem(label, value, color);
        }
    }

    public int? Width
    {
        get => Content.Width;
        set
        {
            Content.Width = value;
            Invalidate();
        }
    }

    public string? Label
    {
        get => Content.Label;
        set
        {
            Content.Label = value;
            Invalidate();
        }
    }
    
    public bool CenterLabel
    {
        get => Content.LabelAlignment == Justify.Center;
        set
        {
            Content.LabelAlignment = value ? Justify.Center : Justify.Left;
            Invalidate();
        }
    }

    public BarChart AddItem(string label, double value, Color? color = null)
    {
        var newChart = CloneContent();
        newChart.Data.AddRange(Content.Data);
        newChart.AddItem(label, value, color);
        Content = newChart;
        return this;
    }
    
    public BarChart RemoveItem(string label)
    {
        var newChart = CloneContent();
        newChart.Data.AddRange(Content.Data);
        var itemToRemove = newChart.Data.Find(item => item.Label == label);
        if (itemToRemove != null)
        {
            newChart.Data.Remove(itemToRemove);
            Content = newChart;
        }
        return this;
    }

    public BarChart ClearData()
    {
        var newChart = CloneContent();
        // Data is empty by default
        Content = newChart;
        return this;
    }

    public double this[string label]
    {
        get
        {
            var item = Content.Data.Find(i => i.Label == label);
            if (item == null) throw new KeyNotFoundException($"Item with label '{label}' not found.");
            return item.Value;
        }
        set
        {
            var newChart = CloneContent();
            newChart.Data.AddRange(Content.Data);

            var index = newChart.Data.FindIndex(i => i.Label == label);
            if (index != -1)
            {
                var oldItem = newChart.Data[index];
                newChart.Data[index] = new BarChartItem(label, value, oldItem.Color);
            }
            else
            {
                newChart.Data.Add(new BarChartItem(label, value, null));
            }
            Content = newChart;
        }
    }

    public double[] this[params string[] labels]
    {
        get
        {
            var values = new double[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                var label = labels[i];
                var item = Content.Data.Find(item => item.Label == label);
                if (item == null) throw new KeyNotFoundException($"Item with label '{label}' not found.");
                values[i] = item.Value;
            }
            return values;
        }
        set
        {
            if (labels.Length != value.Length)
            {
                throw new ArgumentException("The number of values must match the number of labels.");
            }

            var newChart = CloneContent();
            newChart.Data.AddRange(Content.Data);

            for (int i = 0; i < labels.Length; i++)
            {
                var label = labels[i];
                var val = value[i];
                var index = newChart.Data.FindIndex(item => item.Label == label);

                if (index != -1)
                {
                    var oldItem = newChart.Data[index];
                    newChart.Data[index] = new BarChartItem(label, val, oldItem.Color);
                }
                else
                {
                    newChart.Data.Add(new BarChartItem(label, val, null));
                }
            }
            Content = newChart;
        }
    }

    protected override Spectre.Console.BarChart CloneContent()
    {
        return new Spectre.Console.BarChart
        {
            Width = Content.Width,
            Label = Content.Label,
            LabelAlignment = Content.LabelAlignment,
            ShowValues = Content.ShowValues,
            Culture = Content.Culture,
            MaxValue = Content.MaxValue,
            ValueFormatter = Content.ValueFormatter
        };
    }
}
