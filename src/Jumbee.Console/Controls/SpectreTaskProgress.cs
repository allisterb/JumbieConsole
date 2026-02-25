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

    public override bool HandlesInput => false;
    #region Fields
    public readonly Progress Progress;
    #endregion

    #region Methods
    public Task Start(Action<ProgressContext> action) => Task.Run(() => Progress.Start(action));

    public Task StartAsync(Func<ProgressContext, Task> action) => Progress.StartAsync(action);

    public Task<T> StartAsync<T>(Func<ProgressContext, Task<T>> action) => Progress.StartAsync(action);

    public Progress AddColumns(params ProgressColumn[] columns) => Progress.Columns(columns);

    // Add a right margin to console if frame present
    protected override void Control_OnInitialization()
    {
        if (HasLayout && HasFrame)
        {
            ansiConsole.Margin = new ConsoleGUI.Space.Offset(0, 0, 1, 0);
        }
       
    }
    
    // Progress control will update console buffer
    protected override void Render() { }

    // Control is assumed to always require painting
    protected override void Validate() {}
    #endregion

}
