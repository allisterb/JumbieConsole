namespace Jumbee.Console.TestDemo;

using System;
using System.Threading;
using System.Threading.Tasks;

using ConsoleGUI;
using ConsoleGUI.Input;
using Spectre.Console;

using Jumbee.Console;
using static Jumbee.Console.Color;

public class Program
{
    static void Main(string[] args) => Test1(args);
    
    static void Test1(string[] args)
    {
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
        var barChart = new Jumbee.Console.BarChart(
            ("Planning", 12, Yellow),
            ("Coding", 54, Green),
            ("Testing", 33, Red)
        );

        barChart.Width = 50;
        barChart.Label = "[green bold]Activity[/]";
        barChart.CenterLabel = true;

        // 3. Tree
        var treeControl = new Jumbee.Console.Tree("Root");
        treeControl.AddNode("[yellow]Foo[/]");
        treeControl.AddNodes("[blue]Bar[/]", "Baz", "Qux");
        
        // Example of adding a subtree (since AddNode takes IRenderable)
        var subTree = new Spectre.Console.Tree("Subtree");
        subTree.AddNode("Leaf 1");
        subTree.AddNode("Leaf 2");
        treeControl.AddNode(subTree);

        // --- Wrap Spectre.Console Controls for ConsoleGUI ---
        var tableControl = new SpectreControl<Spectre.Console.Table>(table);

        tableControl.Content.Border = TableBorder.Rounded;
        // var chartControl = new SpectreControl<Spectre.Console.BarChart>(barChart); // No longer needed
        // var treeControl = new SpectreControl<Spectre.Console.Tree>(root); // Replaced by Jumbee.Console.Tree above

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
        var prompt = new TextPrompt("[yellow]What is your name?[/]", blinkCursor: true);
        prompt.Committed += (sender, name) =>
        {
            spinner.Text = $"Hello, [blue]{name}[/]!";
            spinner.SpinnerType = Spectre.Console.Spinner.Known.Ascii; // Change spinner style on success
        };

        var p = prompt
            .WithAsciiBorder()
            .WithTitle("Write here");
        var grid = new Jumbee.Console.Grid([15, 15], [40, 80], [
            [spinner.WithFrame(borderStyle: BorderStyle.Rounded, fgColor: Red, title: "Spinna benz"), prompt],
            [treeControl, barChart]
        ]);

        // Start the user interface
        UI.Start(grid, 130, 40);
        //UI.Start(internalGrid, width:250, height: 60, isTrueColorTerminal: true);
        // Create a separate timer to update the chartControl content periodically
        var random = new Random();
        var chartTimer = new Timer(_ =>
        {
            // The SpectreControl.Content setter will acquire the lock internally
            var newPlanning = (double)random.Next(10, 30);
            var newCoding = (double)random.Next(40, 70);
            var newTesting = (double)random.Next(20, 40);

            // Update existing items using the bulk-update indexer
            barChart["Planning", "Coding", "Testing"] = [newPlanning, newCoding, newTesting];

        }, null, 0, 100);

        var t = Task.Run(() =>
        {
            // Main loop
            while (true)
            {
                ConsoleManager.ReadInput([p, prompt, new InputListener()]);
                Thread.Sleep(50);
            }
        });
        t.Wait();
    }
    
    static void Test2(string[] args)
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
        var hStack = new HorizontalStackPanel(
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

        var tabpanel = new TabPanel(TabBarDock.Top, controls: [("Tab 1", CreateBox("T-Item 1", Magenta1)), ("Tab 2", CreateBox("T-Item 2", Cyan1))]);

        var vt = new TextLabel(TextLabelOrientation.Horizontal, "hello", Red);
        // --- Main Layout ---
        // Combine them into a grid for display
        var grid = new Jumbee.Console.Grid([20, 10, 20, 20], [60], [
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