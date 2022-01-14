using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rebus.Threading;

/// <summary>
/// The "bottleneck" is a wrapper around <see cref="SemaphoreSlim"/> that makes it easy to decrese the count of a semaphore,
/// increasing it again after having used it.
/// </summary>
public class AsyncBottleneck
{
    readonly SemaphoreSlim _semaphore;

    /// <summary>
    /// Constructs the bottleneck, allowing for <paramref name="maxParallelOperationsToAllow"/> parallel operations
    /// to be performed
    /// </summary>
    public AsyncBottleneck(int maxParallelOperationsToAllow) => _semaphore = new SemaphoreSlim(maxParallelOperationsToAllow);

    /// <summary>
    /// Grabs the semaphore and releases an <see cref="IDisposable"/> that will release it again when disposed
    /// </summary>
    public async Task<IDisposable> Enter(CancellationToken cancellationToken) 
    {
        await _semaphore.WaitAsync(cancellationToken);

        return new Releaser(_semaphore);
    }

    class Releaser : IDisposable
    {
        readonly SemaphoreSlim _semaphore;

        public Releaser(SemaphoreSlim semaphore) => _semaphore = semaphore;

        public void Dispose() => _semaphore.Release();
    }
}