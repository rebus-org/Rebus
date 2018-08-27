using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rebus.Workers.ThreadPoolBased
{
    class DefaultAsyncBackoffStrategy : IAsyncBackoffStrategy
    {
        readonly TimeSpan[] _backoffTimes;

        long _waitTimeTicks;

        /// <summary>
        /// Constructs the backoff strategy with the given waiting times
        /// </summary>
        public DefaultAsyncBackoffStrategy(IEnumerable<TimeSpan> backoffTimes)
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
        public Task WaitNoMessage()
        {
            return InnerWaitAsync();
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

        async Task InnerWaitAsync()
        {
            var backoffTime = GetNextBackoffTime();

            await Task.Delay(backoffTime);
        }

        void InnerWait()
        {
            var backoffTime = GetNextBackoffTime();

            Thread.Sleep(backoffTime);
        }

        TimeSpan GetNextBackoffTime()
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
            return backoffTime;
        }
    }
}