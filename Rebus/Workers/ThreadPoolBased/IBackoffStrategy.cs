using System.Threading;
using System.Threading.Tasks;

namespace Rebus.Workers.ThreadPoolBased;

/// <summary>
/// Implements a strategy with which workers will back off in idle periods. Please note that the <see cref="IBackoffStrategy"/>
/// implementations must be reentrant!
/// </summary>
public interface IBackoffStrategy
{
    /// <summary>
    /// Executes the next wait operation by blocking the thread, possibly advancing the wait cursor to a different wait time for the next time.
    /// This function is called each time a worker thread cannot continue because no more parallel operations are allowed to happen.
    /// </summary>
    /// <param name="token"></param>
    void Wait(CancellationToken token);

    /// <summary>
    /// Executes the next wait operation by blocking the thread, possibly advancing the wait cursor to a different wait time for the next time.
    /// This function is called each time a worker thread cannot continue because no more parallel operations are allowed to happen.
    /// </summary>
    /// <param name="token"></param>
    Task WaitAsync(CancellationToken token);

    /// <summary>
    /// Executes the next wait operation by blocking the thread, possibly advancing the wait cursor to a different wait time for the next time.
    /// This function is called each time no message was received.
    /// </summary>
    /// <param name="token"></param>
    void WaitNoMessage(CancellationToken token);

    /// <summary>
    /// Executes the next wait operation by blocking the thread, possibly advancing the wait cursor to a different wait time for the next time.
    /// This function is called each time no message was received.
    /// </summary>
    /// <param name="token"></param>
    Task WaitNoMessageAsync(CancellationToken token);

    /// <summary>
    /// Blocks the thread for a (most likely longer) while, when an error has occurred
    /// </summary>
    /// <param name="token"></param>
    void WaitError(CancellationToken token);

    /// <summary>
    /// Blocks the thread for a (most likely longer) while, when an error has occurred
    /// </summary>
    /// <param name="token"></param>
    Task WaitErrorAsync(CancellationToken token);

    /// <summary>
    /// Resets the strategy. Is called whenever a message was received.
    /// </summary>
    void Reset();
}