namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Linq;

using ConsoleGUI;

/// <summary>
///  A grid layout with controls arranged in rows and columns.
/// </summary>
public class Grid : Layout<ConsoleGUI.Controls.Grid>
{
    #region Constructors
    /// <summary>
    /// Creates a grid layout with the specified row heights, column heights, and arrays of controls.
    /// </summary>
    /// <param name="rowHeights"></param>
    /// <param name="columnWidths"></param>
    /// <param name="controls"></param>
    /// <exception cref="ArgumentException"></exception>
    public Grid(int[] rowHeights, int[] columnWidths, params IFocusable[][]? controls ) : base(new ConsoleGUI.Controls.Grid())
    {                
        control.Rows = rowHeights.Select(h => new ConsoleGUI.Controls.Grid.RowDefinition(h)).ToArray();
        control.Columns = columnWidths.Select(w => new ConsoleGUI.Controls.Grid.ColumnDefinition(w)).ToArray();
        if (controls is not null)
        {
            if (controls.Length != rowHeights.Length)
            {
                throw new ArgumentException("Number of control rows must match number of row heights.");
            }
            if (controls.Any(r => r.Length != columnWidths.Length))
            {
                throw new ArgumentException("Number of control columns must match number of column widths.");
            }   
            for (int r = 0; r < controls.Length; r++)
            {
                for (int c = 0; c < controls[r].Length; c++)
                {
                    control.AddChild(c, r, controls[r][c].FocusableControl);
                }
            }
        }
    }
    #endregion

    #region Methods
    public void SetChild(int row, int column, IControl child) => control.AddChild(column, row, child);
        
    public override int Rows => control.Rows.Length;

    public override int Columns => control.Columns.Length;

    public override IFocusable this[int row, int column] => (IFocusable) control.GetChild(column, row);
    #endregion   
}
