namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;   

using ConsoleGUI;
using ConsoleGUI.Api;
using ConsoleGUI.Space;
using ConsoleGUI.Input;

/// <summary>
/// Manages the overall UI and provides a paint event for controls to subscribe to.
/// </summary>
public static class UI
{
    #region Methods
    /// <summary>
    /// Initializes the console and starts the UI.
    /// </summary>
    public static Task Start(ILayout layout, int width = 110, int height = 25, int paintInterval = 100, bool isTrueColorTerminal = true)
    {
        if (isRunning) return task;
        if (!isTrueColorTerminal)
        {
            ConsoleManager.Console = new SimplifiedConsole(); ;
        }
        ConsoleManager.Setup();
        ConsoleManager.Resize(new Size(width, height));
        ConsoleManager.Content = layout.CControl;
        UI.layout = layout;
        interval = paintInterval;
        foreach(var c in layout.Controls.Select(lc => lc.FocusableControl))
        {
            if (!controls.Contains(c))
            {
                controls.Add(c);
            }               
        }
        timer = new Timer(OnTick, null, interval, interval);
        var globalInputListener = new GlobalInputListener();
        task = Task.Run(() =>
        {
            // Main input loop
            while (!cancellationToken.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {                                       
                    if (_lock.TryEnter())
                    {
                        var inputEvent = new InputEvent(Console.ReadKey(true));
                        // Invoke global input event
                        globalInputListener.OnInput(inputEvent);
                        _lock.Exit();
                        if (inputEvent.Handled)
                        {
                            return;
                        }
                        else
                        {
                            // Invoke control input events
                            inputEventArgs.InputEvent = inputEvent;
                            layout!.OnInput(inputEventArgs);
                        }
                    }
                }
                else
                {
                    Thread.Sleep(interval / 4);
                }                    
            }
        });
        isRunning = true;
        return task;
    }
    /// <summary>
    /// Stops the UI update loop and disposes of the timer. 
    /// </summary>
    public static void Stop()
    {
        if (!isRunning) return;
        isRunning = false;
        timer?.Dispose();
        timer = null;
        controls.Clear();
        cts.Cancel();   
    }
    
    /// <summary>
    /// Handles periodic timer ticks by redrawing the UI and invoking the <see cref="Paint"/> event, if the lock is available.
    /// </summary>
    private static void OnTick(object? state)
    {
        if (_lock.TryEnter())
        {
            // Resize and redraw UI on console if console size changed
            bool resized = ConsoleManager.AdjustBufferSize();
            
            // Resizing will automatically redraw, so just redraw if resize not needed.
            if (!resized) ConsoleManager.Redraw();

            _lock.Exit();
            // Invoke control paint events
            _Paint?.Invoke(null, paintEventArgs);            
        }        
    }   
    #endregion

    #region Properties
    public static ILayout Layout => layout!;    
    #endregion

    #region Fields   
    private static readonly Lock _lock = new Lock();
    private static PaintEventArgs paintEventArgs = new PaintEventArgs(_lock);
    private static InputEventArgs inputEventArgs = new InputEventArgs(_lock);
    private static Timer? timer;
    private static Task task = Task.CompletedTask;
    private static CancellationTokenSource cts = new CancellationTokenSource();
    private static CancellationToken cancellationToken = cts.Token;
    private static int interval = 100;
    private static bool isRunning;
    private static ILayout? layout;
    private static List<IFocusable> controls = new List<IFocusable>();    
    private static Dictionary<ConsoleKeyInfo, Action> GlobalHotKeys = new Dictionary<ConsoleKeyInfo, Action>
    {
        { HotKeys.CtrlN, Stop }
    };
    #endregion

    #region Events
    private static EventHandler<PaintEventArgs>? _Paint;
    public static event EventHandler<PaintEventArgs> Paint
    {
        add
        {
            _Paint = (EventHandler<PaintEventArgs>?)Delegate.Combine(_Paint, value);
            if (value.Target is IFocusable c)
            {
                if (!controls.Contains(c))
                {
                    controls.Add(c);
                }                
            }           
        }
        remove
        {
            _Paint ??= (EventHandler<PaintEventArgs>?)Delegate.Remove(_Paint, value);
            if (value.Target is IFocusable control)
            {
                controls.Remove(control);
            }           
        }
    }
    #endregion

    #region Types
    public class PaintEventArgs : EventArgs
    {
        public readonly Lock Lock;

        public PaintEventArgs(Lock lockObject)
        {
            Lock = lockObject;
        }
    }

    public class InputEventArgs : EventArgs
    {
        public readonly Lock Lock;

        public InputEvent? InputEvent { get; internal set; }

        public InputEventArgs(Lock lockObject)
        {
            Lock = lockObject;
        }
    }

    public class GlobalInputListener: IInputListener
    {
        public void OnInput(InputEvent inputEvent)
        {
            if (GlobalHotKeys.TryGetValue(inputEvent.Key, out var action))
            {               
                action?.Invoke();
                inputEvent.Handled = true;
                return;                
            }           
        }
    }

    public static class HotKeys
    {
        public static ConsoleKeyInfo Ctrl(ConsoleKey key) =>
            new ConsoleKeyInfo((char) (Char.ToLower((char) key) - 96), key, false, false, true);

        public static ConsoleKeyInfo Alt(ConsoleKey key) =>
            new ConsoleKeyInfo(Char.ToLower((char)key), key, false, true, false);

        public static ConsoleKeyInfo CtrlQ = Ctrl(ConsoleKey.Q);
        public static ConsoleKeyInfo CtrlN = Ctrl(ConsoleKey.N);

        public static ConsoleKeyInfo CtrlAltUp = Ctrl(Alt(ConsoleKey.UpArrow).Key);
    }
    #endregion
}


