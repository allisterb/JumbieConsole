namespace Jumbee.Console;

using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Threading.Tasks;
public class SpectreLiveDisplay : Control
{
    public SpectreLiveDisplay(IRenderable target, Action<LiveDisplayContext> start)
    {
        this.target = target;
        this.start = start;
        LiveDisplay = ansiConsole.Live(target);
    }

    public readonly Spectre.Console.LiveDisplay LiveDisplay;
    
    protected IRenderable target;
    
    protected Action<LiveDisplayContext> start; 

    protected override void Render() {}

    protected override void Validate() {}

    protected override void Control_OnInitialization()
    {
        Task.Run(() => LiveDisplay.Start(start));
    }
}
