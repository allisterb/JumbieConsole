namespace Jumbee.Console;

using System;
using System.Threading;
using ConsoleGUI;
using ConsoleGUI.Data;
using ConsoleGUI.Space;

public abstract class Control : CControl, IFocusable, IDisposable    
{
    #region Constructor
    public Control() : base()
    {
        consoleBuffer = new ConsoleBuffer();
        ansiConsole = new AnsiConsoleBuffer(consoleBuffer);
        UI.Paint += OnPaint;
        OnFocus += Control_OnFocus;
        OnLostFocus += Control_OnLostFocus;
    }

    protected virtual void Control_OnLostFocus() {}

    protected virtual void Control_OnFocus() {}
    #endregion

    #region Indexers    
    public override Cell this[Position position]
    {
        get
        {
            lock (UI.Lock)
            {
                if (position.X >= Size.Width || position.Y >= Size.Height)
                {
                    return emptyCell;
                }
                else
                {
                    return consoleBuffer[position];
                }
            }
        }
    }
    #endregion

    #region Properties
    public virtual int Width
    {
        get => Size.Width;
        set
        {
            Resize(new Size(value, Size.Height));
        }
    }

    public virtual int Height
    {
        get => Size.Height;
        set
        {
            Resize(new Size(Size.Width, value));
        }
    }   

    public ControlFrame? Frame
    {
        get => field;
        set
        {
            if (value is not null && UI.HasControl(this))
            {
                UI.AddInputListener(value);
            }
            field = value;
        }

    }

    public virtual bool IsFocused
    {
        get => field;
        set
        {
            var old = field;
            field = value;
            if (field && !old)
            {
                Control_OnFocus();
                OnFocus?.Invoke();
            }
            else if (!field && old)
            {
                Control_OnLostFocus();
                OnLostFocus?.Invoke();
            }
        }
    }

    public IFocusable FocusableControl => this.Frame is not null ? this.Frame : this;

    #endregion

    #region Methods
    public virtual void Dispose()
    {
        UI.Paint -= OnPaint;
    }

    public void Focus() => IsFocused = true;

    public void UnFocus() => IsFocused = false;

    /// <summary>
    /// This method renders the control's content to the console buffer.
    /// </summary>
    /// <remarks>Note that this does not actually draw the control on the console screen. The <see cref="ConsoleGUI.Common.Control.Redraw"/> method indicates to
    /// parent containers that the control has been updated and needs to be redrawn on the console screen.
    /// </remarks>
    protected abstract void Render();

    protected override void Initialize()
    {
        lock (UI.Lock)
        {
            // Handle the case when negative or overflow sizes may get passed down by parent containers
            int width = Math.Min(1000, Math.Max(0, MaxSize.Width));
            int height = Math.Min(1000, Math.Max(0, MaxSize.Height));
            
            var size = new Size(width, height);                             
            Resize(size);
            consoleBuffer.Size = Size;
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
            }
        }
    }

    /// <summary>
    /// Renders the control and indicates to its parent that it should be redrawn and marks the control region as valid..
    /// </summary>
    protected void Paint()
    {
        // Render the control's content to the console buffer.
        Render();
        // Notify parent containers that the control needs to be redrawn.
        Redraw();
        // Mark the control region as valid.
        Validate();
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
    
    #region Events
    public event FocusableEventHandler? OnFocus;
    public event FocusableEventHandler? OnLostFocus;
    #endregion

    #region Fields
    protected static readonly Cell emptyCell = new Cell(Character.Empty);
    protected internal uint paintRequests;
    protected readonly ConsoleBuffer consoleBuffer;
    protected readonly AnsiConsoleBuffer ansiConsole;
    #endregion
}
