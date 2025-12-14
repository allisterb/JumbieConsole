namespace Jumbee.Console;

using System;
using System.Threading;

using ConsoleGUI.Common;
using ConsoleGUI.Data;
using ConsoleGUI.Space;
using Spectre.Console.Rendering;

using ConsoleGuiSize = ConsoleGUI.Space.Size;

/// <summary>
/// Wraps a Spectre <see cref="IRenderable"/> control for use with ConsoleGUI layout types. 
/// </summary>
/// <remarks>
/// Uses an <see cref="AnsiConsoleBuffer"/> to render the control to a buffer.
/// Public property setters and methods that affect a control's visual state should call <see cref="Invalidate"/> to request a re-render on the next UI update tick.
/// </remarks>
/// <typeparam name="T"></typeparam>
public class SpectreControl<T> : Control, IDisposable where T : IRenderable
{
    #region Constructors
    public SpectreControl(T content)
    {
        _content = content;
        _bufferConsole = new ConsoleBuffer();
        _ansiConsole = new AnsiConsoleBuffer(_bufferConsole);
        UI.Paint += OnPaint;
    }
    #endregion
    
    #region Properties
    public T Content
    {
        get => _content;
        set 
        {
            _content = value;
            Invalidate();
        }
    }
    #endregion

    #region Indexers
    public sealed override Cell this[Position position]
    {
        get
        {
            lock (UI.Lock)
            {
                if (_bufferConsole.Buffer == null || position.X < 0 || position.X >= Size.Width || position.Y < 0 || position.Y >= Size.Height)
                {
                    return _emptyCell;
                }
                else
                {
                    return _bufferConsole.Buffer[position.X, position.Y];
                }
            }
        }
    }
    #endregion

    #region Methods
    public void Dispose()
    {
        UI.Paint -= OnPaint;
    }

    protected sealed override void Initialize()
    {
        lock (UI.Lock)
        {
            // Resize the control to fill the available space
            // We clip it to avoid issues if MaxSize is 'infinite' (though unlikely in this specific layout)
            var targetSize = MaxSize;
            targetSize = new ConsoleGuiSize(Math.Max(0, targetSize.Width), Math.Max(0, targetSize.Height));

            if (targetSize.Width > 1000) targetSize = new ConsoleGuiSize(1000, targetSize.Height);
            if (targetSize.Height > 1000) targetSize = new ConsoleGuiSize(targetSize.Width, 1000);

            Resize(targetSize);

            // Resize buffer using safe dimensions (Size can be negative if Min/Max limits are invalid)
            _bufferConsole.Resize(new ConsoleGuiSize(Math.Max(0, Size.Width), Math.Max(0, Size.Height)));

            Render();
            Redraw();
        }
    }

    /// <summary>
    /// Indicates the control should be repainted on the next UI update tick.
    /// </summary>
    protected void Invalidate() => Interlocked.Increment(ref paintRequests);

    /// <summary>
    /// Creates a copy of the current instance's control content.
    /// </summary>
    /// <remarks>This method is intended to be overridden in a derived class to clone the content of the current instance including all properties that can be modified by the user. 
    /// By default, it throws a <see /// cref="NotImplementedException"/>.
    /// </remarks>
    /// <returns>A new instance of type <typeparamref name="T"/> that is a copy of the current instance's content.</returns>
    /// <exception cref="NotImplementedException">Thrown if the method is not overridden in a derived class.</exception>
    protected virtual T CloneContent() => throw new NotImplementedException($"Cloning not implemented for type {typeof(T).Name}. Override CloneContent() in derived class.");

    /// <summary>
    /// Handles the pain event triggered by the UI timer.
    /// </summary>
    /// <remarks>This method tries to implement thread-safe rendering by locking on the provided synchronization object.
    /// If one or more render requests are pending, it triggers the rendering process and resets the render request
    /// count.</remarks>
    /// <param name="sender">The source of the event. This parameter can be <see langword="null"/>.</param>
    /// <param name="e">An instance of <see cref="PaintEventArgs"/> containing event data, including a synchronization lock.</param>
    private void OnPaint(object? sender, UI.PaintEventArgs e)
    {
        lock (e.Lock)
        {
            if (paintRequests > 0)
            {
                Render();
                Redraw();
                Interlocked.Exchange(ref paintRequests, 0u);
            }
        }
    }

    /// <summary>
    /// Renders the control's content to the console buffer.
    /// </summary>
    /// <remarks>This method clears the console buffer and writes the control's content using the full width
    /// of the control.  If the control's size is invalid (width or height less than or equal to zero), the method exits
    /// without performing any rendering.
    /// This does not actually draw to the console screen, it just updates the buffer. The ConsoleGUI <see cref="ConsoleGUI.ConsoleManager"/> 
    /// handles drawing the buffer on the console screen.
    /// </remarks>
    private void Render()
    {
        if (Size.Width <= 0 || Size.Height <= 0)
        {
            return;
        }
        
        // Render Spectre content to buffer
        _ansiConsole.Clear(true);
        // We probably want to render with the full width of the control
        // Spectre will look at the Profile.Width which comes from the IConsole.Size (BufferConsole.Size)
        _ansiConsole.Write(_content);        
    }
    #endregion

    #region Fields
    private readonly ConsoleBuffer _bufferConsole;
    private readonly AnsiConsoleBuffer _ansiConsole;
    private T _content;
    private uint paintRequests;
    private static readonly Cell _emptyCell = new Cell(Character.Empty);
    #endregion
}
