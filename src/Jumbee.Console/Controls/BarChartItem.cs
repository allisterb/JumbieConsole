using System;
using System.Collections.Generic;
using System.Text;

namespace Jumbee.Console;

public struct BarChartItem
{   
    public BarChartItem(BarChart chart, int index, string label, double value, Color color)
    {
        this.Index = index;
        this.Label = label;
        this.Value = value;
        this.Color = color;
        this.chart = chart;
        UpdateChart();
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
            chart?.UpdateItemLabel(Index, value);
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
