namespace Jumbee.Console;

using System;
using System.Threading;

public static class UIUpdate
{
    private static Timer? _timer;
    private static int _interval = 100;
    private static readonly object _internalLock = new object();
    private static bool _isRunning;

    public static readonly object Lock = new object();

    public static event EventHandler<UIUpdateTimerEventArgs>? Tick;

    public static void StartTimer(int intervalMs = 100)
    {
        lock (_internalLock)
        {
            if (_isRunning) return;
            _interval = intervalMs;
            _isRunning = true;
            _timer = new Timer(OnTick, null, _interval, _interval);
        }
    }

    public static void StopTimer()
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
            Tick?.Invoke(null, new UIUpdateTimerEventArgs(Lock));
        }
    }
}

public class UIUpdateTimerEventArgs : EventArgs
{
    public readonly object Lock;

    public UIUpdateTimerEventArgs(object lockObject)
    {
        Lock = lockObject;
    }
}
