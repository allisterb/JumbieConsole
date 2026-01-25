namespace Jumbee.Console;

using System;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;

/// <summary>
/// Collects and stores performance metrics (CPU and Memory usage) using System.Diagnostics.Metrics.
/// </summary>
public class PerformanceMetrics : IDisposable
{
    private readonly int _historySize;
    private readonly double[] _cpuReadings;
    private readonly long[] _memoryReadings;
    private int _cpuIndex;
    private int _memoryIndex;
    private int _cpuCount;
    private int _memoryCount;
    
    private MeterListener? _listener;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceMetrics"/> class.
    /// </summary>
    /// <param name="historySize">The number of most recent measurements to store for calculating averages.</param>
    public PerformanceMetrics(int historySize)
    {
        if (historySize <= 0) throw new ArgumentOutOfRangeException(nameof(historySize), "History size must be greater than zero.");

        _historySize = historySize;
        _cpuReadings = new double[historySize];
        _memoryReadings = new long[historySize];
        _cpuIndex = 0;
        _memoryIndex = 0;
        _cpuCount = 0;
        _memoryCount = 0;
    }

    /// <summary>
    /// Starts recording performance metrics.
    /// </summary>
    internal void Start()
    {
        if (_listener != null) return; // Already started

        _listener = new MeterListener();
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "System.Runtime")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        _listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "cpu-usage")
            {
                lock (_cpuReadings)
                {
                    _cpuReadings[_cpuIndex] = measurement;
                    _cpuIndex = (_cpuIndex + 1) % _historySize;
                    if (_cpuCount < _historySize) _cpuCount++;
                }
            }
        });

        _listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "working-set")
            {
                lock (_memoryReadings)
                {
                    _memoryReadings[_memoryIndex] = measurement;
                    _memoryIndex = (_memoryIndex + 1) % _historySize;
                    if (_memoryCount < _historySize) _memoryCount++;
                }
            }
        });

        _listener.Start();
        
        // Initiate recording
        _listener.RecordObservableInstruments();
    }

    /// <summary>
    /// Stops recording performance metrics.
    /// </summary>
    internal void Stop()
    {
        _listener?.Dispose();
        _listener = null;
    }

    /// <summary>
    /// Gets the average CPU usage percentage from the recorded history.
    /// </summary>
    public double AverageCpuUsage
    {
        get
        {
            lock (_cpuReadings)
            {
                return _cpuCount > 0 ? _cpuReadings.Take(_cpuCount).Average() : 0;
            }
        }
    }

    /// <summary>
    /// Gets the average Working Set memory usage in bytes from the recorded history.
    /// </summary>
    public double AverageMemoryUsage
    {
        get
        {
            lock (_memoryReadings)
            {
                return _memoryCount > 0 ? _memoryReadings.Take(_memoryCount).Average() : 0;
            }
        }
    }

    /// <summary>
    /// Manually triggers a measurement recording.
    /// </summary>
    public void Capture()
    {
        _listener?.RecordObservableInstruments();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        Stop();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}
