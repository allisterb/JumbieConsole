namespace Jumbee.Console;

using System;
using System.Linq;
using ConsoleGUI;

public class VerticalStackPanel : Layout<ConsoleGUI.Controls.VerticalStackPanel>
{
    public VerticalStackPanel(params IFocusable[]? controls) : base(new ConsoleGUI.Controls.VerticalStackPanel())
    {
        if (controls != null)
        {
            foreach (var control in controls)
            {
                this.control.Add(control);
            }
        }
        UpdateInputListeners();
    }

    public void Add(params IFocusable[] controls)
    {
        foreach (var control in controls)
        {
            this.control.Add(control);
        }
        UpdateInputListeners();
    }

    public void Remove(params IFocusable[] controls)
    {
        foreach (var control in controls)
        {
            this.control.Remove(control);
        }
        UpdateInputListeners();
    }

    public override int Rows => control.Children.Count();

    public override int Columns => 1;

    public override IFocusable this[int row, int column]
    {
        get
        {
            if (column != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(column));
            }
            return (IFocusable) control.Children.ElementAt(row);
        }
    }
}
