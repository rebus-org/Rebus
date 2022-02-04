using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rebus.Logging;

namespace Rebus.Threading.TaskParallelLibrary;

/// <summary>
/// Implementation of <see cref="IAsyncTaskFactory"/> that uses TPL to execute the background task
/// </summary>
public class TplAsyncTaskFactory : IAsyncTaskFactory
{
    readonly IRebusLoggerFactory _rebusLoggerFactory;

    /// <summary>
    /// Creates a new TPL-based async task factory
    /// </summary>
    public TplAsyncTaskFactory([NotNull] IRebusLoggerFactory rebusLoggerFactory) => _rebusLoggerFactory = rebusLoggerFactory ?? throw new ArgumentNullException(nameof(rebusLoggerFactory));

    /// <summary>
    /// Creates a new async task
    /// </summary>
    public IAsyncTask Create(string description, Func<Task> action, bool prettyInsignificant = false, int intervalSeconds = 10)
    {
        return new TplAsyncTask(description, action, _rebusLoggerFactory, prettyInsignificant)
        {
            Interval = TimeSpan.FromSeconds(intervalSeconds)
        };
    }
}