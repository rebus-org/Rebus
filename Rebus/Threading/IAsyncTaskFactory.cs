using System;
using System.Threading.Tasks;

namespace Rebus.Threading;

/// <summary>
/// Factory that is capable of creating lightweight async tasks for doing background work
/// </summary>
public interface IAsyncTaskFactory
{
    /// <summary>
    /// Creates a new async task
    /// </summary>
    IAsyncTask Create(string description, Func<Task> action, bool prettyInsignificant = false, int intervalSeconds = 10);
}