namespace Jumbee.Console;

using ConsoleGUI;
using System;

public enum TabBarDock
{
    Top,
    Left,
    Right,
    Bottom
}

public class TabPanel : Layout<TabPanelDockPanel>
{
    public TabPanel(TabBarDock tabBarDock, Color activeTabBgColor = default, Color inactiveTabBgColor = default, params (string, IFocusable)[] controls) : base(new TabPanelDockPanel(tabBarDock, inactiveTabBgColor)) 
    {
        foreach (var (tabname, tabcontrol) in controls)
        {
            this.control.AddTab(tabname, tabcontrol.FocusableControl, activeTabBgColor, inactiveTabBgColor);
        }
        UpdateInputListeners();
    }
            
    public override int Rows { get; } = 1;

    public override int Columns => this.control.TabCount;

    public override IFocusable this[int row, int column]
    {
        get
        {
            if (row != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(row));                
            }
            return (IFocusable) control[column];
        }
    }       
}

