using System;
using System.Timers;
using Rebus.Logging;

namespace Rebus.Timers
{
    public class BackgroundTimer
    {
        static ILog _log;

        static BackgroundTimer()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        public static IDisposable Schedule(Action callback, TimeSpan interval, TimeSpan errorBackoffTime = default(TimeSpan))
        {
            return NewSystemTimersTimer(callback, interval, errorBackoffTime);
        }

        static TimerInstance NewSystemTimersTimer(Action callback, TimeSpan interval, TimeSpan errorBackoffTime)
        {
            var currentlyExecutingCallback = false;
            var timer = new Timer {Interval = interval.TotalMilliseconds};
            var lastError = DateTime.MinValue;

            timer.Elapsed += delegate
            {
                if (currentlyExecutingCallback) return;

                lock (timer)
                {
                    if (currentlyExecutingCallback) return;
                    if (lastError + errorBackoffTime > DateTime.UtcNow) return;

                    try
                    {
                        callback();
                    }
                    catch (Exception exception)
                    {
                        _log.Warn("An error occurred while executing callback: {0} - will wait {1} before trying again", exception, errorBackoffTime);
                        lastError = DateTime.UtcNow;
                    }
                    finally
                    {
                        currentlyExecutingCallback = false;
                    }
                }
            };
            timer.Start();
            return new TimerInstance(timer);
        }

        class TimerInstance : IDisposable
        {
            readonly IDisposable _disposable;

            public TimerInstance(IDisposable disposable)
            {
                _disposable = disposable;
            }

            public void Dispose()
            {
                _disposable.Dispose();
            }
        }
    }
}