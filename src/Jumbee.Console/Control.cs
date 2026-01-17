namespace Jumbee.Console;

using System;
using System.Threading;

using ConsoleGUI.Data;
using ConsoleGUI.Input;
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
    #endregion

    #region Properties
    public virtual int Width
    {
        get => field;
        set
        {
            field = value;            
            Resize(new Size(value, Height));
        }
    }

    public virtual int Height
    {
        get => field;
        set
        {
            field = value;
            Resize(new Size(Width, value));
        }
    }   

    public ControlFrame? Frame { get; set; }

    public bool Focusable { get; set; } = true;

    public bool IsFocused
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                Frame?.IsFocused = value;   
                if (value)
                    OnFocus?.Invoke();
                else
                    OnLostFocus?.Invoke();
            }
        }
    }

    public IFocusable FocusableControl => this.Frame is not null ? this.Frame : this;

    public virtual bool HandlesInput { get; } = false;

    public void OnInput(UI.InputEventArgs inputEventArgs)
    {
        if (HandlesInput)
        {
            lock(inputEventArgs.Lock)
            {
                (this as IFocusable).OnInput(inputEventArgs.InputEvent);
            }
        }
    }
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
        var (width, height) = CalculateSize();
        var size = new Size(width, height);                             
        Resize(size);
        consoleBuffer.Size = Size;
        Paint();        
    }
            
    protected virtual void Paint() => Render();

    /// <summary>
    /// Indicates the control should be repainted on the next UI update tick.
    /// </summary>    
    protected virtual void Invalidate() => Interlocked.Increment(ref paintRequests);

    /// <summary>
    /// Indicates that any pending paint requests have been handled.
    /// </summary>
    protected virtual void Validate() => Interlocked.Exchange(ref paintRequests, 0u);

    /// <summary>
    /// Calculates the size of the control based on its own dimensions and the maximum size constraints set by paremt.
    /// </summary>
    /// <returns></returns>
    protected (int, int) CalculateSize()
    {
        // Handle the case when negative or overflow sizes may get passed down by parent containers
        int maxWidth = Math.Clamp(MaxSize.Width, 0 ,1000);
        int maxHeight = Math.Clamp(MaxSize.Height, 0, 1000);

        // Use Width and Height as preferred if set (non-zero), otherwise default to MaxSize.Width and MaxSize.Height set by parents
        var preferredWidth = Width > 0 ? Math.Clamp(Width, 0, 1000) : maxWidth;
        var preferredHeight = Height > 0 ? Math.Clamp(Height, 0, 1000) : maxHeight;
        
        var width = Math.Min(preferredWidth, maxWidth);
        var height = Math.Min(preferredHeight, maxHeight);
        return (width, height);
    }

    /// <summary>
    /// Handles the paint event triggered by the UI timer.
    /// </summary>
    /// <remarks>
    /// This method tries to implement thread-safe painting by always running inside a lock on the provided synchronization object.
    /// If one or more paint requests are pending, it runs the painting process and resets the paint request count.
    /// </remarks>
    /// <param name="sender">The source of the event. This parameter can be <see langword="null"/>.</param>
    /// <param name="e">An instance of <see cref="PaintEventArgs"/> containing event data, including a synchronization lock.</param>
    private void OnPaint(object? sender, UI.PaintEventArgs e)
    {
        if (paintRequests > 0)
        {
            lock (e.Lock)
            {
                Paint();
                Validate();
            }
        }
    }

    private void OnInput(object? sender, UI.InputEventArgs inputEventArgs)
    {
        lock (inputEventArgs.Lock)
        {
            (this as IInputListener).OnInput(inputEventArgs.InputEvent);
        }
    }
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
