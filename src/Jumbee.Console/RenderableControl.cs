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
    #region Methods
    Measurement IRenderable.Measure(RenderOptions options, int maxWidth) => this.Measure(options, maxWidth);

    IEnumerable<Segment> IRenderable.Render(RenderOptions options, int maxWidth) => this.Render(options, maxWidth);

    protected abstract IEnumerable<Segment> Render(RenderOptions options, int maxWidth);

    [DebuggerStepThrough]
    protected virtual Measurement Measure(RenderOptions options, int maxWidth) => new Measurement(maxWidth, maxWidth);

    protected override void Initialize()
    {
        lock (UI.Lock)
        {
            // Handle the case when negative or overflow sizes may get passed down by parent containers
            int maxWidth = Math.Min(1000, Math.Max(0, MaxSize.Width));
            int maxHeight = Math.Min(1000, Math.Max(0, MaxSize.Height));

            // Create RenderOptions based on the virtual console and max width and height
            var options = new RenderOptions(ansiConsole.Profile.Capabilities, new Spectre.Console.Size(maxWidth, maxHeight));

            // Determine Spectre.Console control measurement
            var measurement = Measure(options, maxWidth);

            // Determine final size
            // Respect MinSize/MaxSize constraints from ConsoleGUI parent
            var width = Math.Clamp(measurement.Max, MinSize.Width, MaxSize.Width);

            // Height might be determined by the content (if available) or calculated
            // For many widgets, height is dynamic. 
            // If Measure doesn't return height, we might need a test Render or heuristics.
            // Assuming we fit in MaxSize.Height:
            var height = Math.Min(measurement.Max, MaxSize.Height); // Simplified

            // Resize the ConsoleGUI control            
            var size = new ConsoleGUI.Space.Size(width, height);
            Resize(size);

            // Update buffer size
            consoleBuffer.Size = Size;

            // Trigger Paint/Render to fill the buffer
            Paint();
        }
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
