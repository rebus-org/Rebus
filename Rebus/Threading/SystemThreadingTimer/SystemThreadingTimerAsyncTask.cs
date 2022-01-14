using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Logging;

namespace Rebus.Threading.SystemThreadingTimer;

/// <summary>
/// Implementation of <see cref="IAsyncTask"/> that uses a <see cref="Timer"/> to schedule callbacks
/// </summary>
public class SystemThreadingTimerAsyncTask : IAsyncTask
{
    /// <summary>
    /// This is the default interval between invocations if the periodic action, unless the <see cref="Interval"/> property is set to something else
    /// </summary>
    public static TimeSpan DefaultInterval = TimeSpan.FromSeconds(10);

    readonly string _description;
    readonly Func<Task> _action;
    readonly bool _prettyInsignificant;
    readonly ILog _log;

    Timer _timer;
    bool _disposed;
    TimeSpan _interval;

    readonly object _tickExecutionLock = new object();
    volatile bool _executingTick;

    /// <summary>
    /// Constructs the periodic background task with the given <paramref name="description"/>, periodically executing the given <paramref name="action"/>,
    /// waiting <see cref="Interval"/> between invocations.
    /// </summary>
    public SystemThreadingTimerAsyncTask(string description, Func<Task> action, IRebusLoggerFactory rebusLoggerFactory, bool prettyInsignificant)
    {
        _log = rebusLoggerFactory.GetLogger<SystemThreadingTimerAsyncTask>();
        _description = description;
        _action = action;
        _prettyInsignificant = prettyInsignificant;
        Interval = DefaultInterval;
    }

    /// <summary>
    /// Configures the interval between invocations. The default value is <see cref="DefaultInterval"/>
    /// </summary>
    public TimeSpan Interval
    {
        get { return _interval; }
        set
        {
            _interval = value < TimeSpan.FromMilliseconds(100)
                ? TimeSpan.FromMilliseconds(100)
                : value;
        }
    }

    /// <summary>
    /// Starts the task
    /// </summary>
    public void Start()
    {
        LogStartStop("Starting periodic task {taskDescription} with interval {timerInterval}", _description, Interval);

        _timer = new Timer(obj => Tick(), null, Interval, Interval);
    }

    async void Tick()
    {
        if (_executingTick) return;

        lock (_tickExecutionLock)
        {
            if (_executingTick) return;

            _executingTick = true;
        }

        try
        {
            await _action();
        }
        catch (Exception exception)
        {
            _log.Warn("Exception in periodic task {taskDescription}: {exception}", _description, exception);
        }
        finally
        {
            _executingTick = false;
        }
    }

    /// <summary>
    /// Stops the background task
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            if (_timer == null) return;

            LogStartStop("Stopping periodic task {taskDescription}", _description);

            _timer.Dispose();
        }
        finally
        {
            _disposed = true;
        }
    }

    void LogStartStop(string message, params object[] objs)
    {
        if (_prettyInsignificant)
        {
            _log.Debug(message, objs);
        }
        else
        {
            _log.Info(message, objs);
        }
    }
}