using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Logging;

namespace Rebus.Threading
{
    /// <summary>
    /// <see cref="Task"/>-based background timer thingie, that will periodically call an async <see cref="Func&lt;Task&gt;"/>
    ///  </summary>
    public class AsyncTask : IDisposable
    {
        static ILog _log;

        static AsyncTask()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        /// <summary>
        /// This is the default interval between invocations if the periodic action, unless the <see cref="Interval"/> property is set to something else
        /// </summary>
        public static TimeSpan DefaultInterval = TimeSpan.FromSeconds(10);

        readonly string _description;
        readonly Func<Task> _action;
        readonly bool _prettyInsignificant;
        readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        readonly ManualResetEvent _finished = new ManualResetEvent(false);

        Task _task;

        bool _disposed;
        TimeSpan _interval;

        /// <summary>
        /// Constructs the periodic background task with the given <paramref name="description"/>, periodically executing the given <paramref name="action"/>,
        /// waiting <see cref="Interval"/> between invocations.
        /// </summary>
        public AsyncTask(string description, Func<Task> action, bool prettyInsignificant = false)
        {
            _description = description;
            _action = action;
            _prettyInsignificant = prettyInsignificant;
            Interval = DefaultInterval;
        }

        /// <summary>
        /// Last-resort shutdown of the task (if it wasn't properly disposed)
        /// </summary>
        ~AsyncTask()
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
            }
        }

        /// <summary>
        /// Starts the task
        /// </summary>
        public void Start()
        {
            if (_disposed)
            {
                throw new InvalidOperationException(string.Format("Cannot start periodic task '{0}' because it has been disposed!", _description));
            }

            LogStartStop("Starting periodic task '{0}' with interval {1}", _description, Interval);

            var token = _tokenSource.Token;

            _task = Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        var intervalAboveZero = Interval;

                        await Task.Delay(intervalAboveZero, token);

                        token.ThrowIfCancellationRequested();

                        try
                        {
                            await _action();
                        }
                        catch (TaskCanceledException)
                        {
                            throw;
                        }
                        catch (Exception exception)
                        {
                            _log.Warn("Exception in periodic task '{0}': {1}", _description, exception);
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    _finished.Set();
                }
            }, token);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Cancels the background task so that it stops, waiting (up to 5 seconds) until it has exited properly
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            try
            {
                // if it was never started, we don't do anything
                if (_task == null) return;

                LogStartStop("Stopping periodic task '{0}'", _description);

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
}