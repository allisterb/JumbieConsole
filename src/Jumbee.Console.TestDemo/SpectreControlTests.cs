using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Jumbee.Console;
using Spectre.Console;

namespace Jumbee.Console.TestDemo;

internal class SpectreControlTests
{
    public static void LiveDisplayTests()
    {
        var table = new Table()
 .Border(TableBorder.None)
 .AddColumn("Metric")
 .AddColumn("Value");

        var panel = new Panel(table)
            .Header("System Monitor")
            .BorderColor(Color.Cyan1)
            .RoundedBorder();

        // Adapted from https://spectreconsole.net/console/live/live-display
        var liveDisplay = new SpectreLiveDisplay(table);
        
        
        var grid = new Grid([60], [60], [
            [liveDisplay.WithFrame(title: "Live Display")]
        ]);

        var t = UI.Start(grid);
        liveDisplay.Start(ctx =>
        {
            for (int i = 0; i < 10; i++)
            {
                table.Rows.Clear();
                table.AddRow("CPU Usage", $"{Random.Shared.Next(10, 80)}%");
                table.AddRow("Memory", $"{Random.Shared.Next(2, 8)} GB / 16 GB");
                table.AddRow("Network", $"{Random.Shared.Next(100, 999)} MB/s");
                table.AddRow("Uptime", $"{i + 1} seconds");

                ctx.Refresh();
                Thread.Sleep(1000);
            }
        });

        t.Wait();
    }

    public static void ProgressTests()
    {
        var progress = new SpectreTaskProgress();
        var grid = new Grid([60], [60], [
           [progress.WithFrame(title: "Progress")]
       ]);
        var t = UI.Start(grid); 
        progress.Start(ctx =>
        {
            var task1 = ctx.AddTask("Downloading images", maxValue: 125);
            var task2 = ctx.AddTask("Processing documents", maxValue: 50);
            var task3 = ctx.AddTask("Compiling code"); // maxValue defaults to 100

            while (!ctx.IsFinished)
            {
                task1.Increment(1.5);
                task2.Increment(0.8);
                task3.Increment(1.2);
                Thread.Sleep(50);
            }
        });
        t.Wait();
    }
}
