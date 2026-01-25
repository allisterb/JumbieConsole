namespace Jumbee.Console;

using System;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;

/// <summary>
/// Collects and stores performance metrics (CPU and Memory usage) using System.Diagnostics.Metrics.
/// </summary>
public class ProcessMetrics : IDisposable
{
    private readonly int _historySize;
    private readonly double[] _cpuReadings;
    private readonly long[] _memoryReadings;
    private readonly long[] _heapAllocReadings;
    private readonly long[] _fragmentationReadings;
    private readonly long[] _threadPoolReadings;
    private readonly long[] _lockContentionReadings;

    private int _cpuIndex;
    private int _memoryIndex;
    private int _heapAllocIndex;
    private int _fragmentationIndex;
    private int _threadPoolIndex;
    private int _lockContentionIndex;

    private int _cpuCount;
    private int _memoryCount;
    private int _heapAllocCount;
    private int _fragmentationCount;
    private int _threadPoolCount;
    private int _lockContentionCount;
    
    private MeterListener _listener = new MeterListener();
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessMetrics"/> class.
    /// </summary>
    /// <param name="historySize">The number of most recent measurements to store for calculating averages.</param>
    public ProcessMetrics(int historySize)
    {
        if (historySize <= 0) throw new ArgumentOutOfRangeException(nameof(historySize), "History size must be greater than zero.");

        _historySize = historySize;
        _cpuReadings = new double[historySize];
        _memoryReadings = new long[historySize];
        _heapAllocReadings = new long[historySize];
        _fragmentationReadings = new long[historySize];
        _threadPoolReadings = new long[historySize];
        _lockContentionReadings = new long[historySize];
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "System.Runtime")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        _listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "dotnet.process.cpu.time")
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
            if (instrument.Name == "dotnet.process.memory.working_set")
            {
                lock (_memoryReadings)
                {
                    _memoryReadings[_memoryIndex] = measurement;
                    _memoryIndex = (_memoryIndex + 1) % _historySize;
                    if (_memoryCount < _historySize) _memoryCount++;
                }
            }
            else if (instrument.Name == "dotnet.gc.heap.total_allocated")
            {
                lock (_heapAllocReadings)
                {
                    _heapAllocReadings[_heapAllocIndex] = measurement;
                    _heapAllocIndex = (_heapAllocIndex + 1) % _historySize;
                    if (_heapAllocCount < _historySize) _heapAllocCount++;
                }
            }
            else if (instrument.Name == "dotnet.gc.last_collection.heap.fragmentation.size")
            {
                lock (_fragmentationReadings)
                {
                    _fragmentationReadings[_fragmentationIndex] = measurement;
                    _fragmentationIndex = (_fragmentationIndex + 1) % _historySize;
                    if (_fragmentationCount < _historySize) _fragmentationCount++;
                }
            }
            else if (instrument.Name == "dotnet.thread_pool.thread.count")
            {
                lock (_threadPoolReadings)
                {
                    _threadPoolReadings[_threadPoolIndex] = measurement;
                    _threadPoolIndex = (_threadPoolIndex + 1) % _historySize;
                    if (_threadPoolCount < _historySize) _threadPoolCount++;
                }
            }
            else if (instrument.Name == "dotnet.monitor.lock_contentions")
            {
                lock (_lockContentionReadings)
                {
                    _lockContentionReadings[_lockContentionIndex] = measurement;
                    _lockContentionIndex = (_lockContentionIndex + 1) % _historySize;
                    if (_lockContentionCount < _historySize) _lockContentionCount++;
                }
            }
        });       
    }

    /// <summary>
    /// Starts recording performance metrics.
    /// </summary>
    internal void Start()
    {
        

        _listener.Start();
        _listener.RecordObservableInstruments();
    }

    /// <summary>
    /// Stops recording performance metrics.
    /// </summary>
    internal void Stop()
    {
        _listener?.Dispose();
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
    /// Gets the total bytes allocated on the managed heap (latest recorded value).
    /// </summary>
    public long TotalAllocatedBytes
    {
        get
        {
            lock (_heapAllocReadings)
            {
                // Returns the last recorded value as this is a cumulative counter
                if (_heapAllocCount == 0) return 0;
                int lastIndex = (_heapAllocIndex - 1 + _historySize) % _historySize;
                return _heapAllocReadings[lastIndex];
            }
        }
    }

    /// <summary>
    /// Gets the average GC heap fragmentation in bytes.
    /// </summary>
    public double AverageGcFragmentation
    {
        get
        {
            lock (_fragmentationReadings)
            {
                return _fragmentationCount > 0 ? _fragmentationReadings.Take(_fragmentationCount).Average() : 0;
            }
        }
    }

    /// <summary>
    /// Gets the average number of ThreadPool threads.
    /// </summary>
    public double AverageThreadPoolThreads
    {
        get
        {
            lock (_threadPoolReadings)
            {
                return _threadPoolCount > 0 ? _threadPoolReadings.Take(_threadPoolCount).Average() : 0;
            }
        }
    }

    /// <summary>
    /// Gets the total number of monitor lock contentions (latest recorded value).
    /// </summary>
    public long TotalLockContentions
    {
        get
        {
            lock (_lockContentionReadings)
            {
                // Returns the last recorded value as this is a cumulative counter
                if (_lockContentionCount == 0) return 0;
                int lastIndex = (_lockContentionIndex - 1 + _historySize) % _historySize;
                return _lockContentionReadings[lastIndex];
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
