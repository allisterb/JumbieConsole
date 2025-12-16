namespace Jumbee.Console;

using ConsoleGUI.Data;
using ConsoleGUI.Space;
using System;
using System.Threading;
using System.Threading.Tasks;

using ConsoleGuiSize = ConsoleGUI.Space.Size;

public abstract class Control : ConsoleGUI.Common.Control, IDisposable    
{
    #region Constructor
    public Control() : base()
    {
        consoleBuffer = new ConsoleBuffer();
        ansiConsole = new AnsiConsoleBuffer(consoleBuffer);
        UI.Paint += OnPaint;
    }
    #endregion

    #region Indexers    
    public override Cell this[Position position]
    {
        get
        {
            lock (UI.Lock)
            {
                if (consoleBuffer.Buffer == null || position.X < 0 || position.X >= Size.Width || position.Y < 0 || position.Y >= Size.Height)
                {
                    return _emptyCell;
                }
                else
                {
                    return consoleBuffer.Buffer[position.X, position.Y];
                }
            }
        }
    }
    #endregion
    
    #region Methods
    public virtual void Dispose()
    {
        UI.Paint -= OnPaint;
    }

    /// <summary>
    /// This method renders the control's content to the console buffer.
    /// </summary>
    protected abstract void Render();

    protected sealed override void Initialize()
    {
        lock (UI.Lock)
        {
            var targetSize = MaxSize;
            targetSize = new ConsoleGuiSize(Math.Max(0, targetSize.Width), Math.Max(0, targetSize.Height));

            if (targetSize.Width > 1000) targetSize = new ConsoleGuiSize(1000, targetSize.Height);
            if (targetSize.Height > 1000) targetSize = new ConsoleGuiSize(targetSize.Width, 1000);
            Resize(targetSize);
            consoleBuffer.Resize(new ConsoleGuiSize(Math.Max(0, Size.Width), Math.Max(0, Size.Height)));
            Paint();
        }
    }

    /// <summary>
    /// Handles the paint event triggered by the UI timer.
    /// </summary>
    /// <remarks>
    /// This method tries to implement thread-safe painting by locking on the provided synchronization object.
    /// If one or more paint requests are pending, it runs the painting process and resets the paint request count.
    /// </remarks>
    /// <param name="sender">The source of the event. This parameter can be <see langword="null"/>.</param>
    /// <param name="e">An instance of <see cref="PaintEventArgs"/> containing event data, including a synchronization lock.</param>
    protected virtual void OnPaint(object? sender, UI.PaintEventArgs e)
    {
        lock (e.Lock)
        {
            if (paintRequests > 0)
            {
                Paint();
                Validate();
            }
        }
    }

    protected void Paint()
    {
        Render();
        Redraw();
    }

    /// <summary>
    /// Indicates the control should be repainted on the next UI update tick.
    /// </summary>    
    protected void Invalidate() => Interlocked.Increment(ref paintRequests);

    /// <summary>
    /// Indicates that any pending paint requests have been handled.
    /// </summary>
    protected void Validate() => Interlocked.Exchange(ref paintRequests, 0u);
    #endregion

    #region Fields
    protected static readonly Cell _emptyCell = new Cell(Character.Empty);
    protected internal uint paintRequests;
    protected readonly ConsoleBuffer consoleBuffer;
    protected readonly AnsiConsoleBuffer ansiConsole;
    #endregion
}
