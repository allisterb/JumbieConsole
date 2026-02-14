namespace Jumbee.Console.TestDemo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using ConsoleGUI;
using ConsoleGUI.Input;

using Vezel.Cathode.Text.Control;


using Jumbee.Console;
using static Jumbee.Console.Style;


public class Program
{
    static async Task Main(string[] args)
    {
        //GridTest(args);
        //GridTest(args);
        //SpectreControlTests.LiveDisplayTests();
        DockPanelTest(args);
        //SpectreControlTests.ProgressTests();
        Console.Clear();
        Console.WriteLine("Average UI draw time: {0}ms. Average UI paint time: {1}ms.", UI.AverageDrawTime, UI.AveragePaintTime);
        Console.WriteLine("Average control paint times:");
        foreach(var c in UI.AverageControlPaintTimes)
        {
            Console.WriteLine("{0}: {1}ms", c.Key.GetType().Name, c.Value);
        }
        Console.WriteLine("Max control paint times:");
        foreach (var c in UI.MaxControlPaintTimes)
        {
            Console.WriteLine("{0}: {1}ms", c.Key.GetType().Name, c.Value);
        }

        Console.WriteLine($"Average CPU Usage: {UI.ProcessMetrics.AverageCpuUsage:F2}%");
        Console.WriteLine($"Average Memory Usage: {UI.ProcessMetrics.AverageMemoryUsage / 1024 / 1024:F2} MB");
        Console.WriteLine($"Total Allocated: {UI.ProcessMetrics.TotalAllocatedBytes / 1024 / 1024:F2} MB");
        Console.WriteLine($"GC Fragmentation: {UI.ProcessMetrics.GcFragmentation}");
        Console.WriteLine($"Average ThreadPool Threads: {UI.ProcessMetrics.ThreadPoolThreads:F2}");
        Console.WriteLine($"Total Lock Contentions: {UI.ProcessMetrics.TotalLockContentions}");
    }

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
        var table3 = new Spectre.Console.Table()
                .AddColumn(new Spectre.Console.TableColumn("Line"));
        var disp = new SpectreLiveDisplay(table3);
        disp.Display.Overflow = Spectre.Console.VerticalOverflow.Ellipsis;
        
        // 3. Tree
        var treeControl = new Tree("Root", guide: TreeGuide.BoldLine)
        {
            SelectedForegroundColor = Color.White,
            SelectedBackgroundColor = Color.Blue
        };
        treeControl.AddNode("Foo").AddChildren("Bar", "Baz", "Qux");

        // Example of adding a subtree (since AddNode takes IRenderable)
        //var subTree = new Tree(rootText: "Subtree");
        //subTree.AddNode("Leaf 1");
        //subTree.AddNode("Leaf 2");
        //treeControl.AddNode(subTree);

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
        var prompt = new TextPrompt("[yellow]What is your name?[/]", blinkCursor: false) { Width = 20};
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
            [disp, barChart]
        ]);
        //treeControl.IsFocused = true;
        // Start the user interface
        p.Focus();
        var t = UI.Start(grid, 130, 40);
        disp.Start((ctx) =>
        {
            for (int i = 1; i <= 100; i++)
            {
                //ctx.
                table3.Rows.Add([new Spectre.Console.Markup($"Line {i}")]);
                ctx.Refresh();
                Thread.Sleep(50);
            }
        });
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

        }, null, 0, 50);

        t.Wait();        
    }
    
    static void ListBoxTest(string[] args)
    {
        var listBox = new ListBox
        {
            SelectedForegroundColor = Color.White,
            SelectedBackgroundColor = Color.DarkMagenta
        };
        listBox.AddItem("[red]Item 1 (Markup)[/]");
        listBox.AddItem("Item 2 (Green FG)", Color.Green);
        listBox.AddItem("Item 3 (Blue BG)", background: Color.Blue);
        listBox.AddItem("Item 4 (Yellow on Navy)", Color.Yellow, Color.Navy);

        var dynamicItem = listBox.AddItem("I will change color...", Color.White);

        var prompt = new TextPrompt("[yellow]Type a command (add/remove/clear/color/exit):[/]", blinkCursor: true) { Width = 80 };
        prompt.Committed += (sender, text) =>
        {
            if (text.StartsWith("add "))
            {
                var content = text.Substring(4);
                listBox.AddItem(content);
            }
            else if (text == "color")
            {
                dynamicItem.ForegroundColor = Color.FromSpectreColor(Spectre.Console.Color.FromInt32(new Random().Next(0, 255)));
                dynamicItem.Text = $"My color is now: {dynamicItem.ForegroundColor?.ToString() ?? "Default"}";
            }
            else if (text == "remove")
            {
                var items = listBox.Items.ToList();
                if (items.Count > 0)
                {
                    listBox.RemoveItem(items.Last());
                }
            }
            else if (text == "clear")
            {
                listBox.Clear();
            }
            else if (text == "exit")
            {
                Environment.Exit(0);
            }
        };

        var grid = new Jumbee.Console.Grid([20], [50, 50], [
            [listBox.WithRoundedBorder(Purple).WithHeight(3).WithWidth(5), prompt.WithFrame(title: "Controls")]
        ]);
        
        listBox.IsFocused = true;

        var t = UI.Start(grid);
        
        // Add some dynamic items automatically
        var random = new Random();
        var timer = new Timer(_ =>
        {
            var r = random.Next(0, 100);
            if (r < 30)
            {
                listBox.AddItem($"Auto Item {DateTime.Now.Second}");
            }
        }, null, 0, 1000);

        t.Wait();
    }
    
    static void DockPanelTest(string[] args)
    {
        var p = new TextEditor(TextEditor.Language.Markdown, blinkCursor: true)
           .WithRoundedBorder(Purple)
           .WithTitle("Editor");
        var tree = new Tree("tree", TreeGuide.Line, Green | Dim) { Width = 20, Height=10 };
        tree.AddNodes("Y".WithStyle(Red | Dim), "Z".WithStyle(Blue | Underline)).WithTitle("Functions");
        p.Focus();
        var d = new DockPanel(DockedControlPlacement.Left, tree, p);
        //var g = new Grid([10], [100, 100], [p, tree.WithRoundedBorder(Blue)]);
        var t = UI.Start(d);
        t.Wait();
        Console.WriteLine("Average draw time: {0} Average paint time: {1}.", UI.AverageDrawTime, UI.AveragePaintTime);
        
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

