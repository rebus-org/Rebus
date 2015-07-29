using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Rebus.Logging;

namespace Rebus.Bus
{
    /// <summary>
    /// I can help you wait, especially if you want to wait e.g. for some kind of increasing amount of time.
    /// </summary>
    internal class BackoffHelper
    {
        public bool LoggingDisabled { get; set; }

        static ILog log;

        static BackoffHelper()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        /// <summary>
        /// So we can replace it with something else e.g. during tests
        /// </summary>
        internal Action<TimeSpan> waitAction = timeToWait => Thread.Sleep(timeToWait);

        readonly TimeSpan[] backoffTimes;

        long currentIndex;

        public BackoffHelper(IEnumerable<TimeSpan> backoffTimes)
        {
            var timeSpans = backoffTimes.ToArray();
            if (timeSpans.Length == 0)
            {
                throw new ArgumentException("Backoff helper must be initialized with at least one time span!", "backoffTimes");
            }
            if (timeSpans.Where(t => t <= TimeSpan.FromSeconds(0)).Any())
            {
                throw new ArgumentException(
                    string.Format(
                        "Backoff helper must be initialized with only positive time spans - the following time spans were given: {0}",
                        string.Join(", ", timeSpans)), "backoffTimes");
            }
            this.backoffTimes = timeSpans;
        }

        /// <summary>
        /// Resets the backoff helper which means that waiting will start over from the beginning of the sequence of wait times
        /// </summary>
        public void Reset()
        {
            Interlocked.Exchange(ref currentIndex, 0);
        }

        /// <summary>
        /// Waits the time specified next in the sequence
        /// </summary>
        public void Wait()
        {
            Wait(_ => { });
        }

        /// <summary>
        /// Waits the time specified next in the sequence, invoking the callback with the time that will be waited
        /// </summary>
        public void Wait(Action<TimeSpan> howLongTheWaitWillLast)
        {
            var effectiveIndex = Math.Min(Interlocked.Read(ref currentIndex), backoffTimes.Length - 1);
            var timeToWait = backoffTimes[effectiveIndex];

            if (!LoggingDisabled)
            {
                log.Debug("Waiting {0}", timeToWait);
            }

            howLongTheWaitWillLast(timeToWait);
            waitAction(timeToWait);

            Interlocked.Increment(ref currentIndex);
        }
    }
}