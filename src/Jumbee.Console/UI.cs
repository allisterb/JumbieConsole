namespace Jumbee.Console;

using ConsoleGUI;
using ConsoleGUI.Api;
using ConsoleGUI.Input;
using ConsoleGUI.Space;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;   

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
        ProcessMetrics.Start();
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
                        if (!inputEvent.Handled)
                        {
                            // Invoke control input events
                            inputEventArgs.InputEvent = inputEvent;
                            layout.OnInput(inputEventArgs);
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
        ProcessMetrics.Stop();
    }
    
    /// <summary>
    /// Handles periodic timer ticks by redrawing the UI and invoking the <see cref="Paint"/> event, if the lock is available.
    /// </summary>
    private static void OnTick(object? state)
    {
        if (_lock.TryEnter())
        {
            // Draw UI on screen            
            ConsoleManager.Draw();
            _lock.Exit();

            // Invoke control paint events
            StartPaintTimer();
            _Paint?.Invoke(null, paintEventArgs);
            StopPaintTimer();
        }        
    }   
    /// <summary>
    /// Executes an action within the UI lock, ensuring thread safety for UI updates.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    internal static void Invoke(Action action)
    {
        if (_lock.IsHeldByCurrentThread)
        {
            action();
        }
        else
        {
            lock (_lock)
            {
                action();
            }
        }
    }
    
    public static void StartPaintTimer() => paintTimer.Restart();

    public static void StopPaintTimer()
    {
        paintTimer.Stop();
        paintTimes[paintTimeIndex] = paintTimer.ElapsedMilliseconds;
        paintTimeIndex = (paintTimeIndex + 1) % paintTimeSamples;
    }
    #endregion

    #region Properties
    public static ILayout Layout => layout!;
    
    public static double AveragePaintTime
    {
        get
        {
            long total = 0;
            int count = 0;
            foreach (var time in paintTimes)
            {
                if (time > 0)
                {
                    total += time;
                    count++;
                }
            }
            return count > 0 ? (double)total / count : 0;
        }
    }

    public static double AverageDrawTime => ConsoleManager.AverageDrawTime;

    public static IDictionary<IFocusable, double> AverageControlPaintTimes
    {
        get
        {
            var d = new Dictionary<IFocusable, double>();   
            foreach(var c in controlPaintTimes)
            {
                long total = 0;
                int count = 0;
                foreach (var time in c.Value)
                {
                    if (time.HasValue)
                    {
                        total += time.Value;
                        count++;
                    }
                }
                d[c.Key] = count > 0 ? (double)total / count : 0;
            }
            return d;
        }
    }

    public static IDictionary<IFocusable, long> MaxControlPaintTimes => controlPaintTimes
        .Select(kv => KeyValuePair.Create(kv.Key, kv.Value.Where(v => v.HasValue).Select(v => v!.Value).DefaultIfEmpty().Max()))
        .ToDictionary();
    #endregion

    #region Events
    private static EventHandler<PaintEventArgs>? _Paint;
    public static event EventHandler<PaintEventArgs> Paint
    {
        add
        {            
            if (value.Target is IFocusable c)
            {
                if (!controls.Contains(c))
                {
                    controls.Add(c);                 
                    controlPaintTimers[c] = new Stopwatch();
                    controlPaintTimes[c] = new long?[paintTimeSamples];                    
                    _Paint = (EventHandler<PaintEventArgs>?)Delegate.Combine(_Paint, value);
                }                
            }           
        }
        remove
        {            
            if (value.Target is IFocusable c)
            {
                _Paint ??= (EventHandler<PaintEventArgs>?)Delegate.Remove(_Paint, controlPaintEventHandlers[c]);
                controls.Remove(c);
            }           
        }
    }
    #endregion

    #region Fields   
    public static readonly ProcessMetrics ProcessMetrics = new ProcessMetrics(300);
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
        { HotKeys.CtrlQ, Stop }
    };
    private static readonly int paintTimeSamples = 60;
    private static readonly long[] paintTimes = new long[paintTimeSamples];
    private static readonly Stopwatch paintTimer = new Stopwatch();
    internal static int paintTimeIndex = 0;
    internal static Dictionary<IFocusable, Stopwatch> controlPaintTimers = new();
    internal static Dictionary<IFocusable, long?[]> controlPaintTimes = new();
    private static Dictionary<IFocusable, EventHandler<PaintEventArgs>> controlPaintEventHandlers = new();
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


