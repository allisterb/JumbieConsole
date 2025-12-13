using System;
using System.Threading;

using ConsoleGUI;
using ConsoleGUI.Controls;
using ConsoleGUI.Space;
using ConsoleGUI.Input;
using Jumbee.Console;
using Jumbee.Console.Controls;
using Spectre.Console;
using Spectre.Console.Rendering;
using Jumbee.Console.Prompts;

using ConsoleGuiSize = ConsoleGUI.Space.Size;
using ConsoleGuiColor = ConsoleGUI.Data.Color;
using SpectreColor = Spectre.Console.Color;
using LayoutGrid = ConsoleGUI.Controls.Grid;



class Program
{
    static void Main(string[] args)
    {
        // Setup ConsoleGUI
        ConsoleManager.Setup();
        ConsoleManager.Resize(new ConsoleGuiSize(120, 40));

        // --- Spectre.Console Controls ---
        // 1. Table
        var table = new Table();
        table.Title("[bold yellow]Jumbee Console[/]");
        table.AddColumn("Library");
        table.AddColumn("Role");
        table.AddColumn("Status");
        table.AddRow("Spectre.Console", "Widgets & Styling", "[green]Integrated[/]");
        table.AddRow("ConsoleGUI", "Layout & Windowing", "[blue]Integrated[/]");
        table.AddRow("Jumbee", "The Bridge", "[bold red]Working![/]");
        table.Border(TableBorder.DoubleEdge);

        // 2. Bar Chart
        var barChart = new Jumbee.Console.Controls.BarChart()
            .AddItem("Planning", 12, SpectreColor.Yellow)
            .AddItem("Coding", 54, SpectreColor.Green)
            .AddItem("Testing", 33, SpectreColor.Red);
        
        barChart.Width = 50;
        barChart.Label = "[green bold]Activity[/]";
        barChart.CenterLabel = true;

        // 3. Tree
        var root = new Tree("Root");
        var foo = root.AddNode("[yellow]Foo[/]");
        var bar = foo.AddNode("[blue]Bar[/]");
        bar.AddNode("Baz");
        bar.AddNode("Qux");
        var quux = root.AddNode("Quux");
        quux.AddNode("Corgi");
        
        // --- Wrap Spectre.Console Controls for ConsoleGUI ---
        var tableControl = new SpectreControl<Spectre.Console.Table>(table);
        // var chartControl = new SpectreControl<Spectre.Console.BarChart>(barChart); // No longer needed
        var treeControl = new SpectreControl<Spectre.Console.Tree>(root);

        // --- ConsoleGUI Controls ---
        // Spinner
        var spinner = new Jumbee.Console.Spinner
        {
            SpinnerType = Spectre.Console.Spinner.Known.Dots,
            Text = "Waiting for input...",
            Style = Spectre.Console.Style.Parse("green bold")
        };
        spinner.Start();

        // The TextPrompt control
        var prompt = new ConsoleGUITextPrompt<string>("[yellow]What is your name?[/]", enableCursorBlink: true);
        prompt.Committed += (sender, name) => 
        {
            spinner.Text = $"Hello, [blue]{name}[/]!";
            //spinner.Spinner = Spinner.Known.Ascii; // Change spinner style on success
        };
        
        // --- ConsoleGUI Layout ---
        // Use a Grid for layout
        var grid = new LayoutGrid
        {
            Columns = new[]
            {
                new LayoutGrid.ColumnDefinition(60),
                new LayoutGrid.ColumnDefinition(50)
            },
            Rows = new[]
            {
                new LayoutGrid.RowDefinition(15),
                new LayoutGrid.RowDefinition(20),
                new LayoutGrid.RowDefinition(40)
            }
        };

        // Add controls to grid
        grid.AddChild(0, 1, new Margin { Offset = new Offset(1, 1, 1, 1), Content = prompt }); // Top Left
        grid.AddChild(0, 0, new Margin { Offset = new Offset(1, 1, 1, 1), Content = spinner });  // Top Right
        grid.AddChild(1, 0, new Margin { Offset = new Offset(1, 1, 1, 1), Content = barChart }); // Bottom Left
        grid.AddChild(1, 1, new Margin { Offset = new Offset(1, 1, 1, 1), Content = tableControl }); // Bottom Left
        grid.AddChild(1, 2, new Margin { Offset = new Offset(1, 1, 1, 1), Content = treeControl }); // Bottom Left
        
        ConsoleManager.Content = grid;

        // Start the global animation timer
        UIUpdate.StartTimer();

        // Create a separate timer to update the chartControl content periodically
        var random = new Random();
        var chartTimer = new Timer(_ => 
        {
            // The SpectreControl.Content setter will acquire the lock internally
            var newPlanning = random.Next(10, 30);
            var newCoding = random.Next(40, 70);
            var newTesting = random.Next(20, 40);

            // Update existing items using the indexer
            barChart["Planning"] = newPlanning;
            barChart["Coding"] = newCoding;
            barChart["Testing"] = newTesting;

        }, null, 0, 100);

        // Main loop
        while (true)
        {
            ConsoleManager.ReadInput([prompt, new InputListener()]);
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
            Environment.Exit(0);
        }
    }
}
