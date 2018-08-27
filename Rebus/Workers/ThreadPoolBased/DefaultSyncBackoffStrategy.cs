using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        public Task Wait()
        {
            return InnerWait();
        }

        /// <inheritdoc />
        public Task WaitNoMessage()
        {
            return InnerWait();
        }

        /// <inheritdoc />
        public async Task WaitError()
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        /// <inheritdoc />
        public Task Reset()
        {
            Interlocked.Exchange(ref _waitTimeTicks, 0);
	        return Task.FromResult(0);
        }

        async Task InnerWait()
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

            await Task.Delay(backoffTime);
        }
    }
}