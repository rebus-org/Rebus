using System;

namespace Rebus.Threading;

/// <summary>
/// A task that will be executed periodically. Starts executing as soon as <see cref="Start"/>
/// is called, beginning with waiting the full interval before the first execution. Stops running when it is disposed.
/// </summary>
public interface IAsyncTask : IDisposable
{
    /// <summary>
    /// Starts the task
    /// </summary>
    void Start();
}