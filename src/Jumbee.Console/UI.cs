namespace Jumbee.Console;

using System;
using System.Threading;

using ConsoleGUI;
using ConsoleGUI.Api;
using ConsoleGUI.Common;
using ConsoleGUI.Space;

/// <summary>
/// Manages the overall UI update loop and provides a paint event for controls to subscribe to.
/// </summary>
public static class UI
{
    #region Methods
    /// <summary>
    /// Initializes the system console and starts the UI update loop.
    /// </summary>
    /// <param name="layout"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="paintInterval"></param>
    /// <param name="isTrueColorTerminal"></param>
    public static void Start(IControl layout, int width = 110, int height = 25, int paintInterval = 100, bool isTrueColorTerminal = true)
    {        
        if (isRunning) return;        
        if (!isTrueColorTerminal)
        {
            ConsoleManager.Console = new SimplifiedConsole(); ;
        }        
        ConsoleManager.Setup();
        ConsoleManager.Resize(new Size(width, height));
        ConsoleManager.Content = layout;
        interval = paintInterval;                   
        timer = new Timer(OnTick, null, interval, interval);
        isRunning = true;
    }

    public static void Start<T>(Layout<T> layout, int width = 110, int height = 25, int paintInterval = 100, bool isTrueColorTerminal = true) where T : Control, IDrawingContextListener =>
        Start(layout.control, width, height, paintInterval, isTrueColorTerminal);

    /// <summary>
    /// Stops the UI update loop and disposes of the timer. 
    /// </summary>
    public static void Stop()
    {
        if (!isRunning) return;
        isRunning = false;
        timer?.Dispose();
        timer = null;        
    }

    /// <summary>
    /// Handles periodic timer ticks by invoking the <see cref="Paint"/> event, if the lock is available.
    /// </summary>
    /// <remarks>This method attempts to acquire a lock on the <see cref="Lock"/> object. If successful, it
    /// releases the lock immediately and raises the <see cref="Paint"/> event with the predefined <see
    /// cref="paintEventArgs"/>.</remarks>
    /// <param name="state">An optional state object passed by the timer. This parameter is not used in the method.</param>
    private static void OnTick(object? state)
    {
        if (Monitor.TryEnter(Lock))
        {
            Monitor.Exit(Lock);
            Paint?.Invoke(null, paintEventArgs);
        }
    }
    #endregion
    
    #region Fields   
    internal static readonly object Lock = new object();
    private static PaintEventArgs paintEventArgs = new PaintEventArgs(Lock);
    private static Timer? timer;
    private static int interval = 100;
    private static bool isRunning;
    #endregion

    #region Events
    public static event EventHandler<PaintEventArgs>? Paint;
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
    #endregion
}


