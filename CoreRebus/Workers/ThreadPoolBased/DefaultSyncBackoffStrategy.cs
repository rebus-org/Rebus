using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Rebus.Workers.ThreadPoolBased
{
    class DefaultSyncBackoffStrategy : ISyncBackoffStrategy
    {
        readonly TimeSpan[] _backoffTimes;

        long _waitTimeTicks;

        /// <summary>
        /// Constructs the backoff strategy with the given waiting times
        /// </summary>
        public DefaultSyncBackoffStrategy(IEnumerable<TimeSpan> backoffTimes)
        {
            if (backoffTimes == null) throw new ArgumentNullException(nameof(backoffTimes));

            _backoffTimes = backoffTimes.ToArray();

            if (_backoffTimes.Length < 1)
            {
                throw new ArgumentException("Cannot construct customized backoff strategy without specifying at least one wait time!");
            }
        }

        /// <inheritdoc />
        public void Wait()
        {
            InnerWait();
        }

        /// <inheritdoc />
        public void WaitNoMessage()
        {
            InnerWait();
        }

        /// <inheritdoc />
        public void WaitError()
        {
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }

        /// <inheritdoc />
        public void Reset()
        {
            Interlocked.Exchange(ref _waitTimeTicks, 0);
        }

        void InnerWait()
        {
            var waitedSinceTicks = Interlocked.Read(ref _waitTimeTicks);

            if (waitedSinceTicks == 0)
            {
                waitedSinceTicks = DateTime.UtcNow.Ticks;
                Interlocked.Exchange(ref _waitTimeTicks, waitedSinceTicks);
            }

            var waitDurationTicks = DateTime.UtcNow.Ticks - waitedSinceTicks;
            var totalSecondsIdle = (int) TimeSpan.FromTicks(waitDurationTicks).TotalSeconds;
            var waitTimeIndex = Math.Max(0, Math.Min(totalSecondsIdle, _backoffTimes.Length - 1));

            var backoffTime = _backoffTimes[waitTimeIndex];

            Thread.Sleep(backoffTime);
        }
    }
}