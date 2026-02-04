namespace Jumbee.Console;

using System;
using System.Linq;
using ConsoleGUI;

public class HorizontalStackPanel : Layout<ConsoleGUI.Controls.HorizontalStackPanel>
{
    public HorizontalStackPanel(params IFocusable[]? controls) : base(new ConsoleGUI.Controls.HorizontalStackPanel())
    {
        if (controls != null)
        {
            foreach (var control in controls)
            {
                this.control.Add(control.FocusableControl);
            }
        }       
    }

    public void Add(params IFocusable[] controls)
    {
        foreach (var control in controls)
        {
            this.control.Add(control);
        }        
    }

    public void Remove(params IFocusable[] controls)
    {
        foreach (var control in controls)
        {
            this.control.Remove(control);
        }       
    }

    public override int Rows => 1;

    public override int Columns => control.Children.Count();

    public override IFocusable this[int row, int column]
    {
        get
        {
            if (row != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(row));
            }
            return (IFocusable) control.Children.ElementAt(column);
        }
    }
}
