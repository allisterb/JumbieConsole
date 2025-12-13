using System;
using System.Threading;

using ConsoleGUI;
using ConsoleGUI.Controls;
using ConsoleGUI.Space;
using ConsoleGUI.Input;
using Jumbee.Console;
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
        var barChart = new BarChart()
            .Width(50)
            .Label("[green bold]Activity[/]")
            .CenterLabel()
            .AddItem("Planning", 12, SpectreColor.Yellow)
            .AddItem("Coding", 54, SpectreColor.Green)
            .AddItem("Testing", 33, SpectreColor.Red);

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
        var chartControl = new SpectreControl<Spectre.Console.BarChart>(barChart);
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
        
        // Add live update for the BarChart
        UIUpdate.Tick += (sender, e) =>
        {
            lock (e.Lock)
            {
                // Generate new random data for the bar chart
                var random = new Random();
                var newPlanning = random.Next(10, 30);
                var newCoding = random.Next(40, 70);
                var newTesting = random.Next(20, 40);

                var updatedBarChart = new BarChart()
                    .Width(50)
                    .Label("[green bold]Activity[/]")
                    .CenterLabel()
                    .AddItem("Planning", newPlanning, SpectreColor.Yellow)
                    .AddItem("Coding", newCoding, SpectreColor.Green)
                    .AddItem("Testing", newTesting, SpectreColor.Red);
                
                chartControl.Content = updatedBarChart;
            }
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
        grid.AddChild(1, 0, new Margin { Offset = new Offset(1, 1, 1, 1), Content = chartControl }); // Bottom Left
        grid.AddChild(1, 1, new Margin { Offset = new Offset(1, 1, 1, 1), Content = tableControl }); // Bottom Left
        grid.AddChild(1, 2, new Margin { Offset = new Offset(1, 1, 1, 1), Content = treeControl }); // Bottom Left
        
        ConsoleManager.Content = grid;

        // Start the global animation timer
        UIUpdate.StartTimer();

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
