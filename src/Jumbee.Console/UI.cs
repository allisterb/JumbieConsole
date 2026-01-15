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
    public static Task Start(ILayout layout, int width = 110, int height = 25, int paintInterval = 100, int inputInterval = 100, bool isTrueColorTerminal = true)
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
        var inputHandler = new GlobalInputListener();
        task = Task.Run(() =>
        {
            // Main input loop
            while (!cancellationToken.IsCancellationRequested)
            {                
                ConsoleManager.ReadInput([inputHandler]);
                Thread.Sleep(inputInterval);
            }
        }, cancellationToken);
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
    /// Handles periodic timer ticks by invoking the <see cref="Paint"/> event, if the lock is available.
    /// </summary>
    /// <param name="state">An optional state object passed by the timer. This parameter is not used in the method.</param>
    private static void OnTick(object? state)
    {
        if (Monitor.TryEnter(Lock))
        {
            // Resize and redraw UI if console size changed
            bool resized = ConsoleManager.AdjustBufferSize();
            
            // If not resized then just redraw
            if (!resized) ConsoleManager.Redraw();

            Monitor.Exit(Lock);            
            _Paint?.Invoke(null, paintEventArgs);            
        }        
    }
    #endregion

    #region Properties
    public static ILayout Layout => layout!;    
    #endregion

    #region Fields   
    internal static readonly object Lock = new object();
    private static PaintEventArgs paintEventArgs = new PaintEventArgs(Lock);
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
        public readonly object Lock;

        public PaintEventArgs(object lockObject)
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

            else
            {
                layout!.OnInput(inputEvent);
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


