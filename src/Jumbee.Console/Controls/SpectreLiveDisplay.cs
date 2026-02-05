namespace Jumbee.Console;

using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Threading;
using System.Threading.Tasks;

public class SpectreLiveDisplay : Control
{
    public SpectreLiveDisplay(IRenderable target)
    {
        this.target = target;
        LiveDisplay = ansiConsole.Live(target);
    }

    public readonly Spectre.Console.LiveDisplay LiveDisplay;
    
    protected IRenderable target;

    protected override void Render() {}

    protected override void Validate() {}

    public void Start(Action<LiveDisplayContext> action)
    {
        Task.Run(() => LiveDisplay.Start(action));
    }

    public Task StartAsync(Func<LiveDisplayContext, Task> action)
    {
        return LiveDisplay.StartAsync(action);
    }

    public Task<T> StartAsync<T>(Func<LiveDisplayContext, Task<T>> action)
    {
        return LiveDisplay.StartAsync(action);
    }
}
