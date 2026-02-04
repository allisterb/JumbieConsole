namespace Jumbee.Console;

using System;
using ConsoleGUI;

public enum DockedControlPlacement
{
    Top,
    Right,
    Bottom,
    Left
}

public class DockPanel : Layout<ConsoleGUI.Controls.DockPanel>
{
    public DockPanel(DockedControlPlacement placement, IFocusable dockedControl, IFocusable fillControl)
        : base(new ConsoleGUI.Controls.DockPanel())
    {
        this.DockedControl = dockedControl;
        this.FillControl = fillControl;
        control.Placement = placement switch
        {
            DockedControlPlacement.Top => ConsoleGUI.Controls.DockPanel.DockedControlPlacement.Top,
            DockedControlPlacement.Right => ConsoleGUI.Controls.DockPanel.DockedControlPlacement.Right,
            DockedControlPlacement.Bottom => ConsoleGUI.Controls.DockPanel.DockedControlPlacement.Bottom,
            DockedControlPlacement.Left => ConsoleGUI.Controls.DockPanel.DockedControlPlacement.Left,
            _ => throw new NotSupportedException($"Unknown DockedControlPlacement value {placement}.")
        };
    }

    public IFocusable DockedControl
    {
        get => field;
        set
        {
            field = value;
            control.DockedControl = value.FocusableControl;           
        }
    }

    public IFocusable FillControl
    {
        get => field;
        set
        {
            field = value;
            control.FillingControl = value.FocusableControl;            
        }
    }

    public override int Rows => (control.Placement == ConsoleGUI.Controls.DockPanel.DockedControlPlacement.Top ||
                                 control.Placement == ConsoleGUI.Controls.DockPanel.DockedControlPlacement.Bottom) ? 2 : 1;

    public override int Columns => (control.Placement == ConsoleGUI.Controls.DockPanel.DockedControlPlacement.Left ||
                                    control.Placement == ConsoleGUI.Controls.DockPanel.DockedControlPlacement.Right) ? 2 : 1;

    public override IFocusable this[int row, int column]
    {
        get
        {
            if (row < 0 || row >= Rows) throw new ArgumentOutOfRangeException(nameof(row));
            if (column < 0 || column >= Columns) throw new ArgumentOutOfRangeException(nameof(column));

            switch (control.Placement)
            {
                case ConsoleGUI.Controls.DockPanel.DockedControlPlacement.Top:
                    return row == 0 ? DockedControl : FillControl;
                case ConsoleGUI.Controls.DockPanel.DockedControlPlacement.Bottom:
                    return row == 0 ? FillControl : DockedControl;
                case ConsoleGUI.Controls.DockPanel.DockedControlPlacement.Left:
                    return column == 0 ? DockedControl : FillControl;
                case ConsoleGUI.Controls.DockPanel.DockedControlPlacement.Right:
                    return column == 0 ? FillControl : DockedControl;
                default:
                    throw new NotSupportedException($"Unknown DockedControlPlacement value {control.Placement}.");
            }
        }
    }
}
