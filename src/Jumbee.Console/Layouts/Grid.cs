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
    public Grid(int[] rowHeights, int[] columnWidths, params IFocusable[][] controls ) : base(new ConsoleGUI.Controls.Grid())
    {                
        control.Rows = rowHeights.Select(h => new ConsoleGUI.Controls.Grid.RowDefinition(h)).ToArray();
        control.Columns = columnWidths.Select(w => new ConsoleGUI.Controls.Grid.ColumnDefinition(w)).ToArray();
        
        if (controls.Length != rowHeights.Length)
        {
            throw new ArgumentException($"The number of control rows: {controls.Length} must match the number of row heights: {rowHeights.Length}.");
        }
        if (controls.Any(r => r.Length != columnWidths.Length))
        {
            var c = controls.First(r => r.Length != columnWidths.Length);
            var index = Array.IndexOf(controls, c);
            throw new ArgumentException($"The number of control columns in row {index}: {c.Length} must match the number of column widths: {columnWidths.Length}.");
        }   
        for (int r = 0; r < controls.Length; r++)
        {
            for (int c = 0; c < controls[r].Length; c++)
            {
                control.AddChild(c, r, controls[r][c].FocusableControl);
            }
        }
        UpdateInputListeners();
    }
    #endregion

    #region Methods
    public void SetChild(int row, int column, IFocusable child)
    {
        control.AddChild(column, row, child.FocusableControl);
        UpdateInputListeners();
    }
        
    public override int Rows => control.Rows.Length;

    public override int Columns => control.Columns.Length;

    public override IFocusable this[int row, int column] => (IFocusable) control.GetChild(column, row);
    #endregion   
}
