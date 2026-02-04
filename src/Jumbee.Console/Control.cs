namespace Jumbee.Console;

using System;
using System.Threading;

using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.Space;

/// <summary>
/// Base class for all Jumbee.Console controls.
/// </summary>
public abstract class Control : CControl, IFocusable, IDisposable    
{
    #region Constructors
    public Control() : base()
    {
        consoleBuffer = new ConsoleBuffer();
        ansiConsole = new AnsiConsoleBuffer(consoleBuffer);
        UI.Paint += OnPaint;
        OnInitialization += Control_OnInitialization;
        OnFocus += Control_OnFocus;
        OnLostFocus += Control_OnLostFocus;
    }

   
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
            UI.Invoke(() => 
            {                
                field = value;            
                Resize(new Size(value, Height));
            });
        }
    }

    public int ActualWidth => Size.Width;
    
    public virtual int Height
    {
        get => field;
        set
        {
            UI.Invoke(() => 
            {
                field = value;
                Resize(new Size(Width, value));
            });
        }
    }

    public int ActualHeight => Size.Height;

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
                this.OnInput(inputEventArgs.InputEvent!);
            }
        }
    }

    protected virtual void OnInput(InputEvent inputEvent) {}
    #endregion

    #region Methods
    public virtual void Dispose()
    {
        UI.Paint -= OnPaint;
    }
    
    public void Focus() => IsFocused = true;

    public void UnFocus() => IsFocused = false;

    /// <summary>
    /// Fired when a control's Initialize method is called. This method is always called inside UI.Invoke.
    /// </summary>
    protected virtual void Control_OnInitialization() {}

    protected virtual void Control_OnLostFocus() {}

    protected virtual void Control_OnFocus() {}

    /// <summary>
    /// This method renders the control's content to the console buffer.
    /// </summary>
    /// <remarks>Note that this does not actually draw the control on the console screen. 
    /// </remarks>
    protected abstract void Render();

    protected override void Initialize()
    {
        UI.Invoke((() => 
        {
            var (width, height) = CalculateSize();
            var size = new Size(width, height);
            Resize(size);
            consoleBuffer.Size = Size;
            Invalidate();
            OnInitialization?.Invoke();    
        }));
    }                 
            
    /// <summary>
    /// Invoked in the control's OnPaint event handler.
    /// </summary>
    protected virtual void Paint() => Render();

    /// <summary>
    /// Indicates the control should be repainted on the next UI update tick.
    /// </summary>    
    protected virtual void Invalidate() => Interlocked.Increment(ref paintRequests);

    /// <summary>
    /// Indicates that any pending paint requests have been handled and the control does not need re-painting.
    /// </summary>
    protected virtual void Validate() => Interlocked.Exchange(ref paintRequests, 0u);

    /// <summary>
    /// Calculates the size of the control based on its own dimensions and the maximum and minimum size constraints set by its parent.
    /// </summary>
    /// <returns></returns>
    protected (int, int) CalculateSize()
    {
        // Handle the case when negative or overflow sizes may get passed down by parent containers
        int maxWidth = Math.Clamp(MaxSize.Width, 0 ,1000);
        int maxHeight = Math.Clamp(MaxSize.Height, 0, 1000);
        int minWidth = Math.Clamp(MinSize.Width, 0 ,1000);
        int minHeight = Math.Clamp(MinSize.Height, 0, 1000);

        // Use Width and Height as preferred if set (non-zero), otherwise default to MaxSize.Width and MaxSize.Height set by parents
        var preferredWidth = Width > 0 ? Width : Size.Width > 0 ? Size.Width : maxWidth;
        var preferredHeight = Height > 0 ? Height : Size.Height > 0 ? Size.Height : maxHeight;
        
        var width = Math.Clamp(preferredWidth, minWidth, maxWidth);
        var height = Math.Clamp(preferredHeight, minHeight, maxHeight);
        return (width, height);
    }

    public int ClampWidth(int width) => Math.Clamp(width, 0, Size.Width);

    public int ClampHeight(int height) => Math.Clamp(height, 0, Size.Height);
    /// <summary>
    /// Handles the paint event triggered by the UI timer. If one or more paint requests are pending, it runs the painting process and resets the paint request count.
    /// </summary>
    /// <remarks>
    /// This method tries to implement thread-safe painting by always running inside a lock on the provided synchronization object.
    /// </remarks>
    private void OnPaint(object? sender, UI.PaintEventArgs e)
    {
        if (paintRequests > 0)
        {
            lock (e.Lock)
            {
                var timer = UI.controlPaintTimers[this];
                timer.Restart();
                Paint();
                Validate();
                timer.Stop();
                UI.controlPaintTimes[this][UI.paintTimeIndex] = timer.ElapsedMilliseconds;               
            }
        }
        else
        {
            UI.controlPaintTimes[this][UI.paintTimeIndex] = null;
        }
    }    
    #endregion

    #region Events
    public event InitializationHandler OnInitialization;
    public event FocusableEventHandler? OnFocus;
    public event FocusableEventHandler? OnLostFocus;
    #endregion

    #region Fields
    protected static readonly Cell emptyCell = new Cell(Character.Empty);
    protected internal uint paintRequests;
    protected readonly ConsoleBuffer consoleBuffer;
    protected readonly AnsiConsoleBuffer ansiConsole;
    #endregion

    #region Types
    public delegate void InitializationHandler();
    #endregion
}
