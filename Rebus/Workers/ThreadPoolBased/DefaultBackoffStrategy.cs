using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rebus.Workers.ThreadPoolBased
{
    class DefaultBackoffStrategy : IBackoffStrategy
    {
        readonly TimeSpan[] _backoffTimes;

        long _waitTimeTicks;

        /// <summary>
        /// Constructs the backoff strategy with the given waiting times
        /// </summary>
        public DefaultBackoffStrategy(IEnumerable<TimeSpan> backoffTimes)
        {
            if (backoffTimes == null) throw new ArgumentNullException(nameof(backoffTimes));

            _backoffTimes = backoffTimes.ToArray();

            if (_backoffTimes.Length < 1)
            {
                throw new ArgumentException("Cannot construct customized backoff strategy without specifying at least one wait time!");
            }
        }

        /// <param name="token"></param>
        /// <inheritdoc />
        public void Wait(CancellationToken token)
        {
            InnerWait();
        }

        /// <param name="cancellationToken"></param>
        /// <inheritdoc />
        public Task WaitAsync(CancellationToken cancellationToken)
	    {
		    return InnerWaitAsync();
	    }

		/// <inheritdoc />
		public void WaitNoMessage()
	    {
		    InnerWait();
	    }

        /// <param name="token"></param>
        /// <inheritdoc />
        public Task WaitNoMessageAsync(CancellationToken token)
        {
            return InnerWaitAsync();
        }

	    /// <inheritdoc />
	    public void WaitError()
	    {
			Thread.Sleep(TimeSpan.FromSeconds(5));
	    }

        /// <param name="token"></param>
        /// <inheritdoc />
        public async Task WaitErrorAsync(CancellationToken token)
	    {
		    await Task.Delay(TimeSpan.FromSeconds(5));
	    }

        /// <inheritdoc />
        public void Reset()
        {
            Interlocked.Exchange(ref _waitTimeTicks, 0);
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