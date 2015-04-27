using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Logging;

namespace Rebus.Timers
{
    /// <summary>
    /// <see cref="Task"/>-based background timer thingie, that will periodically call an async <see cref="Func&lt;Task&gt;"/>
    ///  </summary>
    public class AsyncPeriodicBackgroundTask : IDisposable
    {
        static ILog _log;

        static AsyncPeriodicBackgroundTask()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        /// <summary>
        /// This is the default interval between invocations if the periodic action, unless the <see cref="Interval"/> property is set to something else
        /// </summary>
        public static TimeSpan DefaultInterval = TimeSpan.FromSeconds(10);

        readonly string _description;
        readonly Func<Task> _action;
        readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        readonly ManualResetEvent _finished = new ManualResetEvent(false);

        Task _task;

        bool _disposed;
        TimeSpan _interval;

        /// <summary>
        /// Constructs the periodic background task with the given <see cref="description"/>, periodically executing the given <see cref="action"/>,
        /// waiting <see cref="Interval"/> between invocations.
        /// </summary>
        public AsyncPeriodicBackgroundTask(string description, Func<Task> action)
        {
            _description = description;
            _action = action;

            Interval = DefaultInterval;
        }

        ~AsyncPeriodicBackgroundTask()
        {
            Dispose(false);
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

                _log.Debug("Periodic task '{0}' interval set to {1}", _description, _interval);
            }
        }

        /// <summary>
        /// Starts the task
        /// </summary>
        public void Start()
        {
            _log.Info("Starting periodic task '{0}' with interval {1}", _description, Interval);

            var token = _tokenSource.Token;

            _task = Task.Factory.StartNew(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        var intervalAboveZero = Interval;

                        await Task.Delay(intervalAboveZero, token);

                        await _action();
                    }
                }
                catch (TaskCanceledException)
                {
                    _finished.Set();
                    throw;
                }
            }, token);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            // if it was never started, we don't do anything
            if (_task == null) return;

            try
            {
                _log.Info("Stopping periodic task '{0}'", _description);

                _tokenSource.Cancel();

                if (!_finished.WaitOne(TimeSpan.FromSeconds(5)))
                {
                    _log.Warn("Periodic task '{0}' did not finish within 5 second timeout!", _description);
                }
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}