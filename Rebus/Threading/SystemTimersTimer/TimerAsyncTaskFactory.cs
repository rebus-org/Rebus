using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Logging;

namespace Rebus.Threading.SystemTimersTimer
{
    /// <summary>
    /// Implementation of <see cref="IAsyncTaskFactory"/> that uses a <see cref="Timer"/> to schedule callbacks
    /// </summary>
    public class TimerAsyncTaskFactory : IAsyncTaskFactory
    {
        readonly IRebusLoggerFactory _rebusLoggerFactory;

        /// <summary>
        /// Constructs the async task factory
        /// </summary>
        public TimerAsyncTaskFactory(IRebusLoggerFactory rebusLoggerFactory)
        {
            _rebusLoggerFactory = rebusLoggerFactory;
        }

        /// <summary>
        /// Creates a new async task
        /// </summary>
        public IAsyncTask Create(string description, Func<Task> action, bool prettyInsignificant = false, int intervalSeconds = 10)
        {
            return new TimerAsyncTask(description, action, _rebusLoggerFactory, prettyInsignificant);
        }
    }
}