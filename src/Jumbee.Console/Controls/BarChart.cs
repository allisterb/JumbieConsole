namespace Jumbee.Console;

using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
        UpdateContent(c =>
        {            
            c.AddItem(label, value, color);
        });
        return this;
    }
    
    public BarChart RemoveItem(string label)
    {
        UpdateContent(c =>
        {            
            var itemToRemove = c.Data.Find(item => item.Label == label);
            if (itemToRemove != null)
            {
                c.Data.Remove(itemToRemove);

            }
        });
        return this;
    }

    public BarChart ClearData()
    {
        UpdateContent(c => c.Data.Clear());
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
            UpdateContent(c =>
            {    
                var index = c.Data.FindIndex(i => i.Label == label);
                if (index != -1)
                {
                    var oldItem = c.Data[index];
                    c.Data[index] = new BarChartItem(label, value, oldItem.Color);
                }
                else
                {
                    c.Data.Add(new BarChartItem(label, value, null));
                }
            });
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

            UpdateContent(c =>
            {                
                for (int i = 0; i < labels.Length; i++)
                {
                    var label = labels[i];
                    var val = value[i];
                    var index = c.Data.FindIndex(item => item.Label == label);

                    if (index != -1)
                    {
                        var oldItem = c.Data[index];
                        c.Data[index] = new BarChartItem(label, val, oldItem.Color);
                    }
                    else
                    {
                        c.Data.Add(new BarChartItem(label, val, null));
                    }
                }
            });            
        }
    }

    protected override Spectre.Console.BarChart CloneContent() => 
        new Spectre.Console.BarChart
        {
            Width = Content.Width,
            Label = Content.Label,
            LabelAlignment = Content.LabelAlignment,
            ShowValues = Content.ShowValues,
            Culture = Content.Culture,
            MaxValue = Content.MaxValue,
            ValueFormatter = Content.ValueFormatter
        }
        .AddItems(Content.Data);
}
