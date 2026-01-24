namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Spectre.Console.Rendering;

/// <summary>
/// A control that implements Spectre.Console.IRenderable
/// </summary>
public abstract class RenderableControl : Control, IRenderable
{
    public RenderableControl() : base() {}
    
    #region Methods
    Measurement IRenderable.Measure(RenderOptions options, int maxWidth) => this.Measure(options, Math.Min(maxWidth, ActualWidth));

    IEnumerable<Segment> IRenderable.Render(RenderOptions options, int maxWidth) => this.Render(options, maxWidth);

    protected abstract IEnumerable<Segment> Render(RenderOptions options, int maxWidth);

    [DebuggerStepThrough]
    protected virtual Measurement Measure(RenderOptions options, int maxWidth) => new Measurement(maxWidth, maxWidth);

    protected override void Initialize()
    {        
        UI.Invoke(() => 
        {
            var (width, height) = CalculateSize();

            // Create RenderOptions based on the virtual console and max width and height
            var options = new RenderOptions(ansiConsole.Profile.Capabilities, new Spectre.Console.Size(width, height));

            // Determine Spectre.Console control measurement
            var measurement = this.Measure(options, width);
            
            // Resize the ConsoleGUI control            
            var size = new ConsoleGUI.Space.Size(width, height);
            Resize(size);

            // Update buffer size
            consoleBuffer.Size = Size;

            Invalidate();        
        });
    }

    /// Renders the control's content to the console buffer.
    /// </summary>
    protected sealed override void Render()
    {
        ansiConsole.Clear(true);
        // We probably want to render with the full width of the control
        // Spectre will look at the Profile.Width which comes from the IConsole.Size (BufferConsole.Size)
        ansiConsole.Write(this);
    }
    #endregion
}
