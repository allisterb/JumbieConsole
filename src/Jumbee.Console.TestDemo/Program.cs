namespace Jumbee.Console.TestDemo;

using System;
using System.Threading;
using System.Threading.Tasks;

using ConsoleGUI;
using ConsoleGUI.Input;

using Vezel.Cathode.Text.Control;


using Jumbee.Console;
using static Jumbee.Console.Style;
using System.Diagnostics;

public class Program
{
    static void Main(string[] args) => GridTest(args);


    static void GridTest(string[] args)
    {
        // --- Spectre.Console Controls ---
        // 1. Table
        var table = new Spectre.Console.Table();
        /*
        table.Title("[bold yellow]Jumbee Console[/]");
        table.AddColumn("Library");
        table.AddColumn("Role");
        table.AddColumn("Status");
        table.AddRow("Spectre.Console", "Widgets & Styling", "[green]Integrated[/]");
        table.AddRow("ConsoleGUI", "Layout & Windowing", "[blue]Integrated[/]");
        table.AddRow("Jumbee", "The Bridge", "[bold red]Working![/]");
        table.Border(TableBorder.DoubleEdge);
        */
        // 2. Bar Chart
        var barChart = new BarChart(ChartOrientation.Horizontal,
            ("Planning", 12, Yellow),
            ("Coding", 54, Green),
            ("Testing", 33, Red)
        )
        {
            BarWidth = 50,
            Label = "[green bold]Activity[/]",
            CenterLabel = true
        };
        // 3. Tree
        var treeControl = new Jumbee.Console.Tree("Root", guide: Jumbee.Console.TreeGuide.Ascii);
        treeControl.AddNode("[yellow]Foo[/]").AddChildren("[blue]Bar[/]", "Baz", "Qux");

        // Example of adding a subtree (since AddNode takes IRenderable)
        var subTree = new Jumbee.Console.Tree("Subtree");
        subTree.AddNode("Leaf 1");
        subTree.AddNode("Leaf 2");
        treeControl.AddNode(subTree);

        // --- Wrap Spectre.Console Controls for ConsoleGUI ---
        //var tableControl = new SpectreControl<Spectre.Console.Table>(table);

        //tableControl.Content.Border = TableBorder.Rounded;
        // var chartControl = new SpectreControl<Spectre.Console.BarChart>(barChart); // No longer needed
        // var treeControl = new SpectreControl<Spectre.Console.Tree>(root); // Replaced by Jumbee.Console.Tree above

        // --- ConsoleGUI Controls ---
        // Spinner
        var spinner = new Spinner
        {
            SpinnerType = Spectre.Console.Spinner.Known.Dots,
            Text = "Waiting for input...",
            Style = Spectre.Console.Style.Parse("green bold")
        };
        spinner.Start();

        // The TextPrompt control
        var prompt = new TextPrompt("[yellow]What is your name?[/]") { Width = 20};
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
        p.IsFocused = true;
        // Start the user interface
        var t = UI.Start(grid, 130, 40);
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

        
        
        var treeTimer = new Timer(_ =>
        {
            treeControl.AddNode("lll");
        }, null, 0, 1000);

        
        t.Wait();
    }
    
    static void DockPanelTest(string[] args)
    {
        var p = new TextPrompt(">", blinkCursor: true)
           .WithRoundedBorder(Purple)
           .WithTitle("Foo")   ;
        var tree = new Tree("tree", TreeGuide.Line, Green | Dim) { Width = 20 };
        tree.AddNodes("Y".WithStyle(Red | Dim), "Z".WithStyle(Blue | Underline)).WithTitle("Functions");
        p.Focus();
        //var d = new DockPanel(DockedControlPlacement.Right, tree, p);
        var g = new Grid([10], [100, 100], [p, tree.WithRoundedBorder(Blue)]);
        var t = UI.Start(g);
        t.Wait();
    }

    static void AnsiControlSequenceBuilderTest(string[] args)
    {
        var cb = new AnsiControlSequenceBuilder();
        var t = new Stopwatch();
        t.Start();
        Console.CursorLeft = Console.CursorLeft + 9;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("This is red text that is really long xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");
        Console.CursorLeft = Console.CursorLeft + 10;
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("This is blue text that is really long x");
        t.Stop();
        Console.WriteLine($"Elapsed: {t.ElapsedMilliseconds} ms");
        t.Reset();
        t.Restart();
        cb.MoveCursorRight(9);
        cb.SetForegroundColor(Red);
        cb.PrintLine("This is red text that is really longs xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");
        cb.MoveCursorRight(10);
        cb.SetForegroundColor(Blue);
        cb.PrintLine("This is blue text that is really long x");
        //Task.Run(cb.WriteToSystemConsole2);
        cb.WriteToSystemConsole();
        t.Stop();
        Console.WriteLine($"Elapsed: {t.ElapsedMilliseconds} ms");
        Thread.Sleep(50);



    }
    /*
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
    */
}

