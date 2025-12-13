namespace Jumbee.Console.Controls;

using System.Collections.Generic;
using Spectre.Console;

public class BarChart : SpectreControl<Spectre.Console.BarChart>
{
    public BarChart() : base(new Spectre.Console.BarChart())
    {
    }

    public int? Width
    {
        get => Content.Width;
        set
        {
            Content.Width = value;
            RequestRender();
        }
    }

    public string? Label
    {
        get => Content.Label;
        set
        {
            Content.Label = value;
            RequestRender();
        }
    }
    
    public bool CenterLabel
    {
        get => Content.LabelAlignment == Justify.Center;
        set
        {
            Content.LabelAlignment = value ? Justify.Center : Justify.Left;
            RequestRender();
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
