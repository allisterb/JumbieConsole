namespace Jumbee.Console;


using System;
using System.Threading;
using System.Threading.Tasks;

using Spectre.Console;
using Spectre.Console.Rendering;

public class SpectreLiveDisplay : Control
{
    public SpectreLiveDisplay(IRenderable target)
    {
        this.target = target;
        Display = ansiConsole.Live(target);
    }

    public readonly LiveDisplay Display;
    
    protected IRenderable target;

    protected override void Render() {}

    protected override void Validate() {}

    public Task Start(Action<LiveDisplayContext> action) => Task.Run(() => Display.Start(action));
    
    public Task StartAsync(Func<LiveDisplayContext, Task> action) => Display.StartAsync(action);
    
    public Task<T> StartAsync<T>(Func<LiveDisplayContext, Task<T>> action) => Display.StartAsync(action);
   
}
