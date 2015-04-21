using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Logging;

namespace Rebus.Timers
{
    public class AsyncPeriodicBackgroundTask : IDisposable
    {
        static ILog _log;

        static AsyncPeriodicBackgroundTask()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        public static TimeSpan DefaultInterval = TimeSpan.FromSeconds(10);

        readonly string _description;
        readonly Func<Task> _action;
        readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        readonly ManualResetEvent _finished = new ManualResetEvent(false);

        public AsyncPeriodicBackgroundTask(string description, Func<Task> action)
        {
            _description = description;
            _action = action;
        }

        public TimeSpan Interval { get; set; }

        public void Start()
        {
            _log.Info("Starting periodic task '{0}' with interval {1}", _description, Interval);

            var token = _tokenSource.Token;

            Task.Factory.StartNew(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        await _action();

                        await Task.Delay(Interval, token);
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
            _log.Info("Stopping periodic task '{0}'", _description);

            _tokenSource.Cancel();

            if (!_finished.WaitOne(TimeSpan.FromSeconds(5)))
            {
                _log.Warn("Periodic task '{0}' did not finish within 5 second timeout!");
            }
        }
    }
}