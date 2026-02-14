namespace Jumbee.Console;

using System;
using System.Threading;
using System.Threading.Tasks;

using Spectre.Console;
using Spectre.Console.Rendering;

/// <summary>
/// A Spectre.Console LiveDisplay widget.
/// </summary>
public class SpectreLiveDisplay : Control
{
    #region Constructors
    public SpectreLiveDisplay(IRenderable target)
    {
        this.target = target;
        Display = ansiConsole.Live(target);
    }
    #endregion

    #region Fields
    public readonly LiveDisplay Display;
    protected IRenderable target;
    #endregion

    #region Methods
    public Task Start(Action<LiveDisplayContext> action) => Task.Run(() => Display.Start(action));

    public Task StartAsync(Func<LiveDisplayContext, Task> action) => Display.StartAsync(action);

    public Task<T> StartAsync<T>(Func<LiveDisplayContext, Task<T>> action) => Display.StartAsync(action);

    // LiveDisplay will update console buffer
    protected override void Render() {}

    // Control is assumed to always require painting
    protected override void Validate() {}
    #endregion

}
