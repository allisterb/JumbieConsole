namespace Jumbee.Console;

using System;
using System.Threading;

using ConsoleGUI;

/// <summary>
/// Manages the overall UI update loop and provides a paint event for controls to subscribe to.
/// </summary>
public static class UI
{    
    #region Methods
    public static void Start(IControl root, int paintInterval = 100)
    {
        lock (_internalLock)
        {
            if (_isRunning) return;
            _interval = paintInterval;
            _isRunning = true;
            ConsoleManager.Content = root;
            _timer = new Timer(OnTick, null, _interval, _interval);             
        }
    }

    public static void Stop()
    {
        lock (_internalLock)
        {
            _isRunning = false;
            _timer?.Dispose();
            _timer = null;
        }
    }

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
    public static event EventHandler<PaintEventArgs>? Paint;
    internal static readonly object Lock = new object();
    private static PaintEventArgs paintEventArgs = new PaintEventArgs(Lock);
    private static Timer? _timer;
    private static int _interval = 100;
    private static readonly object _internalLock = new object();
    private static bool _isRunning;
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


