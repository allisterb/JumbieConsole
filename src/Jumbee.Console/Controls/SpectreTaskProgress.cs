namespace Jumbee.Console;

using System;
using System.Threading;
using System.Threading.Tasks;

using Spectre.Console;
using Spectre.Console.Rendering;

/// <summary>
/// A Spectre.Console Progress widget.
/// </summary>
public class SpectreTaskProgress : Control
{
    #region Constructors
    public SpectreTaskProgress()
    {
        Progress = new Progress(ansiConsole);
    }
    #endregion

    #region Fields
    public readonly Progress Progress;
    #endregion

    #region Methods
    public Task Start(Action<ProgressContext> action) => Task.Run(() => Progress.Start(action));

    public Task StartAsync(Func<ProgressContext, Task> action) => Progress.StartAsync(action);

    public Task<T> StartAsync<T>(Func<ProgressContext, Task<T>> action) => Progress.StartAsync(action);

    public Progress AddColumns(params ProgressColumn[] columns) => Progress.Columns(columns);
    
    // LiveDisplay will update console buffer
    protected override void Render() { }

    // Control is assumed to always require painting
    protected override void Validate() { }
    #endregion

}
