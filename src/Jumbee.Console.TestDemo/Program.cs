namespace Jumbee.Console.TestDemo;

using System;
using System.Threading;

using ConsoleGUI;
using ConsoleGUI.Controls;
using ConsoleGUI.Space;
using ConsoleGUI.Input;
using Jumbee.Console;

using Spectre.Console;

using static Jumbee.Console.Color;

public class Program
{
    static void Main(string[] args)
    {
        // --- Helpers ---
        IControl CreateBox(string text, Jumbee.Console.Color color)
        {
            return new ConsoleGUI.Controls.Background
            {
                Color = color,
                Content = new ConsoleGUI.Controls.Border
                {
                    Content = new ConsoleGUI.Controls.TextBlock
                    {
                        Text = text,
                        Color = Jumbee.Console.Color.Black,
                    }
                }
            };
        }

        // --- 1. HorizontalStackPanel Test ---
        var hStack = new Jumbee.Console.HorizontalStackPanel(
            CreateBox("H-Item 1", Red),
            CreateBox("H-Item 2", Green),
            CreateBox("H-Item 3", Blue)
        );
        //var hStackFrame = hStack.WithFrame(borderStyle: BorderStyle.Single, title: "Horizontal Stack");

        // --- 2. VerticalStackPanel Test ---
        var vStack = new Jumbee.Console.VerticalStackPanel(
            CreateBox("V-Item 1", Cyan1),
            CreateBox("V-Item 2", Magenta1),
            CreateBox("V-Item 3", Yellow)
        );
        //var vStackFrame = vStack.WithFrame(borderStyle: BorderStyle.Single, title: "Vertical Stack");

        // --- 3. DockedControl Test ---
        var dockedContent = CreateBox("Docked (Left)", DarkSlateGray1);
        var fillingContent = CreateBox("Filling Content", White);
        
        var dockedPanel = new Jumbee.Console.DockPanel(
            DockedControlPlacement.Left,
            dockedContent,
            fillingContent
        );
        //var dockedFrame = dockedPanel.WithFrame(borderStyle: BorderStyle.Single, title: "Docked Panel (Left)");

        var tabpanel = new TabPanel(TabBarDock.Right, ("Tab 1", CreateBox("T-Item 1", Magenta1)), ("Tab 2", CreateBox("T-Item 2", Cyan1)));

        var vt = new VerticalTextLabel("hello");
        // --- Main Layout ---
        // Combine them into a grid for display
        var grid = new Jumbee.Console.Grid([10, 10, 20, 20], [60], [
            [tabpanel],
            [hStack],
            [vStack],
            [dockedPanel],
            
        ]);

        //var mainFrame = grid.WithFrame(borderStyle: BorderStyle.Double, title: "Jumbee Console Layout Tests");

        // Start the user interface
        UI.Start(grid);

        // Main loop
        while (true)
        {
            ConsoleManager.ReadInput([new InputListener()]);
            Thread.Sleep(50);
        }
    }
}

public class InputListener : IInputListener
{
    public void OnInput(InputEvent inputEvent)
    {
        if (!inputEvent.Handled)
        {
            if (inputEvent.Key.Key == ConsoleKey.Escape)
            {
                UI.Stop();  
                Environment.Exit(0);
            }
        }
    }
}