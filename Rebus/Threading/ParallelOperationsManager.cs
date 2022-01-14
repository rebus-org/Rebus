using System;
using System.Threading;

namespace Rebus.Threading;

/// <summary>
/// Helper that counts the number of parallel operations. Not reentrant, this bad boy is meant to be used from a single worker thread
/// that may use it to count the number of async parallel operations waiting to be completed
/// </summary>
public class ParallelOperationsManager
{
    readonly int _maxParallelism;
    readonly SemaphoreSlim _semaphore;

    /// <summary>
    /// Constructs the container with the given max number of parallel async operations to allow
    /// </summary>
    public ParallelOperationsManager(int maxParallelism)
    {
        _maxParallelism = maxParallelism;
        _semaphore = new SemaphoreSlim(maxParallelism, maxParallelism);
    }

    /// <summary>
    /// Gets whether any async tasks are currently waiting to be completed
    /// </summary>
    public virtual bool HasPendingTasks => _semaphore.CurrentCount != _maxParallelism;

    /// <summary>
    /// Begins another async operation and returns an <see cref="IDisposable"/> that must be disposed in order to mark the end of the async operation
    /// </summary>
    public ParallelOperation TryBegin()
    {
        var canContinue = _semaphore.Wait(TimeSpan.Zero);

        return new ParallelOperation(canContinue, this);
    }

    void OperationFinished()
    {
        _semaphore.Release(1);
    }

    /// <summary>
    /// Gets a disposable token for the parallel operation - the token indicates whether it's ok to continue
    /// </summary>
    public class ParallelOperation : IDisposable
    {
        readonly bool _canContinue;
        readonly ParallelOperationsManager _parallelOperationsManager;

        bool _disposed;

        internal ParallelOperation(bool canContinue, ParallelOperationsManager parallelOperationsManager)
        {
            _canContinue = canContinue;
            _parallelOperationsManager = parallelOperationsManager;
        }

        /// <summary>
        /// Ends this parallel operation
        /// </summary>
        public void Dispose()
        {
            if (!_canContinue) return;
            if (_disposed) return;

            _parallelOperationsManager.OperationFinished();

            // guard against ever accidentally finishing the operation more than once
            _disposed = true;
        }

        /// <summary>
        /// Gets whether the token was successfully acquired
        /// </summary>
        public bool CanContinue() => _canContinue;
    }
}