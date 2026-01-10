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
/// Manages the overall UI update loop and provides a paint event for controls to subscribe to.
/// </summary>
public static class UI
{
    #region Methods
    /// <summary>
    /// Initializes the system console and starts the UI.
    /// </summary>
    /// <param name="layout"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="paintInterval"></param>
    /// <param name="isTrueColorTerminal"></param>
    public static Task Start(ILayout layout, int width = 110, int height = 25, int paintInterval = 100, bool isTrueColorTerminal = true)
    {        
        if (isRunning) return Task.CompletedTask;        
        if (!isTrueColorTerminal)
        {
            ConsoleManager.Console = new SimplifiedConsole(); ;
        }        
        ConsoleManager.Setup();
        ConsoleManager.Resize(new Size(width, height));
        ConsoleManager.Content = layout;
        interval = paintInterval;                   
        timer = new Timer(OnTick, null, interval, interval);
        var inputHandler = new GlobalInputListener();   
        Task = Task.Run(() =>
        {
            // Main loop
            while (true && !cancellationToken.IsCancellationRequested)
            {
                ConsoleManager.ReadInput([inputHandler]);
                Thread.Sleep(50);
            }
        }, cancellationToken);
        isRunning = true;
        return Task;    
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
    private static Task? Task;
    private static CancellationToken cancellationToken = new CancellationToken();
    private static int interval = 100;
    private static bool isRunning;
    
    private static List<IControl> controls = new List<IControl>();
    private static List<ControlFrame> frames = new List<ControlFrame>();
    private static List<IInputListener> inputListeners = new List<IInputListener>();
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
                controls.Add(c);
                if (c is IInputListener listener)
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
            foreach (var listener in inputListeners)
            {
                if (listener is IFocusable focusable && focusable.IsFocused)
                {
                    listener.OnInput(inputEvent);                    
                }   
            }
        }
    }
    #endregion
}


