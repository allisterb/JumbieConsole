namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Linq;

using ConsoleGUI;
using ConsoleGUI.Controls;
using ConsoleGUI.Input;
using ConsoleGUI.Space;

public class Tab
{    
    #region Constructors
    internal Tab(TabBarDock tabBarDock, string name, IControl content, Color? inactivebgColor = null, Color? activebgColor = null)
    {
        this.inactiveBgColor = inactiveBgColor.Equals(null) ? defaultinactiveBgColor : inactiveBgColor;
        this.activeBgColor = Color.White; //activeBgColor.Equals(null) ? defaultactiveBgColor : activeBgColor;
        bool isHorizontalTabBar = tabBarDock ==  TabBarDock.Top || tabBarDock == TabBarDock.Bottom;
        headerBackground = new Background
        {
            Content = new Margin
            {
                Offset = isHorizontalTabBar ? new Offset(1, 0, 1, 0) : new Offset(0, 1, 0, 1),
                Content = isHorizontalTabBar ? new TextBlock { Text = name } : new VerticalTextLabel(name, this.activeBgColor)
            },
            Color = this.inactiveBgColor

        };

        Header = new Margin
        {
            Offset = new Offset(0, 0, 1, 0),
            Content = headerBackground
        };
        Content = content;

    }
    #endregion
    
    #region Properties
    public IControl Header { get; }
    public IControl Content { get; }
    #endregion

    #region Methods
    public void MarkAsActive() => headerBackground.Color = defaultactiveBgColor;
    public void MarkAsInactive() => headerBackground.Color = defaultinactiveBgColor;
    #endregion

    #region Fields
    private static readonly Color defaultactiveBgColor = new Color(25, 54, 65);
    private static readonly Color defaultinactiveBgColor = new Color(65, 24, 25);
    private readonly Color activeBgColor;
    private readonly Color inactiveBgColor;
    private readonly Background headerBackground;
    #endregion    
}

public class TabPanelDockPanel : ConsoleGUI.Controls.DockPanel
{
    #region Constructors
    internal TabPanelDockPanel(TabBarDock tabBarDock) : base()
    {
        this.tabBarDock = tabBarDock;
        Placement = tabBarDock switch
        {
            TabBarDock.Top => ConsoleGUI.Controls.DockPanel.DockedControlPlacement.Top,
            TabBarDock.Bottom => ConsoleGUI.Controls.DockPanel.DockedControlPlacement.Bottom,
            TabBarDock.Left => ConsoleGUI.Controls.DockPanel.DockedControlPlacement.Left,
            TabBarDock.Right => ConsoleGUI.Controls.DockPanel.DockedControlPlacement.Right,
            _ => throw new NotImplementedException()
        };
            
        tabsPanel = IsHorizontalTabBar ? new ConsoleGUI.Controls.HorizontalStackPanel() : new ConsoleGUI.Controls.VerticalStackPanel();
        DockedControl = new Background
        {
            Color = new Color(25, 25, 52),
            Content = IsHorizontalTabBar ?
            new Boundary
            {
                MinHeight = 1,
                MaxHeight = 1,
                Content = tabsPanel
            } :
            new Boundary
            {
                MinWidth = 1,
                MaxWidth = 1,
                Content = tabsPanel
            }

        };               
    }
    #endregion

    #region Methods
    public void AddTab(string name, IControl content)
    {
        var newTab = new Tab(tabBarDock, name, content);
        tabs.Add(newTab);
        if (IsHorizontalTabBar)
        {
            var htabspanel = (ConsoleGUI.Controls.HorizontalStackPanel)tabsPanel;
            htabspanel.Add(newTab.Header);
        }
        else
        {            
            var vtabspanel = (ConsoleGUI.Controls.VerticalStackPanel)tabsPanel;
            vtabspanel.Add(newTab.Header);            
        }
        if (tabs.Count == 1)
            SelectTab(0);
    }

    public void SelectTab(int tab)
    {
        currentTab?.MarkAsInactive();
        currentTab = tabs[tab];
        currentTab.MarkAsActive();
        FillingControl = currentTab.Content;
    }

    public void OnInput(InputEvent inputEvent)
    {
        if (inputEvent.Key.Key != ConsoleKey.Tab || currentTab is null) return;
        SelectTab((tabs.IndexOf(currentTab) + 1) % tabs.Count);
        inputEvent.Handled = true;
    }
    #endregion

    #region Properties
    public int TabCount => tabs.Count;
    public bool IsHorizontalTabBar => tabBarDock == TabBarDock.Top || tabBarDock == TabBarDock.Bottom;
    #endregion

    #region Indexers
    public IControl this[int t] => tabs.ElementAt(t).Content;
    #endregion

    #region Fields
    private readonly TabBarDock tabBarDock;
    private readonly List<Tab> tabs = new List<Tab>();
    private readonly CControl tabsPanel;
    private Tab? currentTab;
    #endregion
}
