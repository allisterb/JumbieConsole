using System;
using System.Collections.Generic;
using System.Text;

namespace Jumbee.Console;

public struct BarChartItem
{   
    public BarChartItem(BarChart chart, string label, double value, Color color)
    {        
        this.Label = label;
        this.Value = value;
        this.Color = color;
        this.chart = chart;
        UpdateChart();
    }
    public BarChart? chart;
    
    /// <summary>
    /// Gets the item label.
    /// </summary>
    public string Label
    {
        get => field;
        set
        {
            field = value;
            UpdateChart();
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
            UpdateChart();
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
            UpdateChart();
        }
    }

    public void Detach() => chart = null;

    public bool IsDetached => chart is null;

    public void UpdateChart() => chart?.Update();

   
}
