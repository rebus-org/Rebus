using System;
using System.Timers;
using System.Threading.Tasks;
using Rebus.Logging;

namespace Rebus.Threading.SystemThreadingTimer;

/// <summary>
/// Implementation of <see cref="IAsyncTaskFactory"/> that uses a <see cref="Timer"/> to schedule callbacks
/// </summary>
public class SystemThreadingTimerAsyncTaskFactory : IAsyncTaskFactory
{
    readonly IRebusLoggerFactory _rebusLoggerFactory;

    /// <summary>
    /// Constructs the async task factory
    /// </summary>
    public SystemThreadingTimerAsyncTaskFactory(IRebusLoggerFactory rebusLoggerFactory)
    {
        _rebusLoggerFactory = rebusLoggerFactory;
    }

    /// <summary>
    /// Creates a new async task
    /// </summary>
    public IAsyncTask Create(string description, Func<Task> action, bool prettyInsignificant = false, int intervalSeconds = 10)
    {
        return new SystemThreadingTimerAsyncTask(description, action, _rebusLoggerFactory, prettyInsignificant)
        {
            Interval = TimeSpan.FromSeconds(intervalSeconds)
        };
    }
}