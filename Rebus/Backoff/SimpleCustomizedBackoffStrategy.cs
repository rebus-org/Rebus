using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Workers;

namespace Rebus.Backoff
{
    /// <summary>
    /// Simple implementation of <see cref="IBackoffStrategy"/> that is capable of having the backoff times customized
    /// </summary>
    public class SimpleCustomizedBackoffStrategy : IBackoffStrategy
    {
        readonly TimeSpan[] _backoffTimes;

        long _waitTimeTicks;

        /// <summary>
        /// Constructs the backoff strategy with the given waiting times
        /// </summary>
        public SimpleCustomizedBackoffStrategy(IEnumerable<TimeSpan> backoffTimes)
        {
            _backoffTimes = backoffTimes.ToArray();

            if (_backoffTimes.Length < 1)
            {
                throw new ArgumentException("Cannot construct customized backoff strategy without specifying at least one wait time!");
            }
        }

        /// <summary>
        /// Asynchronously executes the next wait operation, possibly advancing the wait cursor to a different wait time for the next time.
        /// This function is called each time no message was received.
        /// </summary>
        public async Task Wait()
        {
            var waitedSinceTicks = Interlocked.Read(ref _waitTimeTicks);

            if (waitedSinceTicks == 0)
            {
                waitedSinceTicks = DateTime.UtcNow.Ticks;
                Interlocked.Exchange(ref _waitTimeTicks, waitedSinceTicks);
            }

            var waitDurationTicks = DateTime.UtcNow.Ticks - waitedSinceTicks;
            var totalSecondsIdle = (int)TimeSpan.FromTicks(waitDurationTicks).TotalSeconds;
            var waitTimeIndex = Math.Min(totalSecondsIdle, _backoffTimes.Length - 1);

            await Task.Delay(_backoffTimes[waitTimeIndex]);
        }

        /// <summary>
        /// Resets the strategy. Is called whenever a message was received.
        /// </summary>
        public void Reset()
        {
            Interlocked.Exchange(ref _waitTimeTicks, 0);
        }
    }
}