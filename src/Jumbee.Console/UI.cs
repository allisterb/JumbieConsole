namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;   

using ConsoleGUI;
using ConsoleGUI.Api;
using ConsoleGUI.Common;
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
        ConsoleManager.Content = layout.LayoutControl;
        interval = paintInterval;
        foreach(var c in layout.Controls)
        {
            if (!controls.Contains(c))
            {
                controls.Add(c);
            }   
            if (c is IInputListener listener && !inputListeners.Contains(listener))
            {
                inputListeners.Add(listener);
            }
        }
        timer = new Timer(OnTick, null, interval, interval);
        var inputHandler = new GlobalInputListener();
        task = Task.Run(() =>
        {
            // Main loop
            while (true && !cancellationToken.IsCancellationRequested)
            {
                ConsoleManager.ReadInput([inputHandler]);
                Thread.Sleep(50);
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

    public static bool HasControl(IControl control) => controls.Contains(control);

    public static void AddInputListener(IInputListener listener)
    {
        if (!inputListeners.Contains(listener))
        {
            inputListeners.Add(listener);
        }
    }   

    /// <summary>
    /// Handles periodic timer ticks by invoking the <see cref="Paint"/> event, if the lock is available.
    /// </summary>
    /// <param name="state">An optional state object passed by the timer. This parameter is not used in the method.</param>
    private static void OnTick(object? state)
    {
        if (Monitor.TryEnter(Lock))
        {
            Monitor.Exit(Lock);            
            _Paint?.Invoke(null, paintEventArgs);
        }
    }
    #endregion

    #region Properties
    private static ILayout Root => (ILayout) ConsoleManager.Content;
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
    
    private static List<IControl> controls = new List<IControl>();    
    private static List<IInputListener> inputListeners = new List<IInputListener>();
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
            if (value.Target is IControl c)
            {
                if (!controls.Contains(c))
                {
                    controls.Add(c);
                }
                if (c is IInputListener listener && !inputListeners.Contains(listener))
                {
                    inputListeners.Add(listener);   
                }
            }           
        }
        remove
        {
            _Paint ??= (EventHandler<PaintEventArgs>?)Delegate.Remove(_Paint, value);
            if (value.Target is IControl control)
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

            foreach (var listener in inputListeners)
            {
                if (listener is IFocusable focusable && focusable.IsFocused)
                {
                    listener.OnInput(inputEvent);                    
                }   
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


