using System;
using System.Threading;

namespace Rebus.Threading
{
    /// <summary>
    /// Helper that counts the number of parallel operations. Not reentrant, this bad boy is meant to be used from a single worker thread
    /// that may use it to count the number of async parallel operations waiting to be completed
    /// </summary>
    public class ParallelOperationsManager
    {
        readonly SemaphoreSlim _semaphore;
        long _currentParallelOperationsCount;

        /// <summary>
        /// Constructs the container with the given max number of parallel async operations to allow
        /// </summary>
        public ParallelOperationsManager(int maxParallelism)
        {
            _semaphore = new SemaphoreSlim(maxParallelism, maxParallelism);
        }

        /// <summary>
        /// Gets whether any async tasks are currently waiting to be completed
        /// </summary>
        public virtual bool HasPendingTasks => Interlocked.Read(ref _currentParallelOperationsCount) > 0;

        /// <summary>
        /// Begins another async operation and returns an <see cref="IDisposable"/> that must be disposed in order to mark the end of the async operation
        /// </summary>
        public ParallelOperation TryBegin()
        {
            if (!_semaphore.Wait(TimeSpan.Zero))
            {
                return new ParallelOperation(false, this);
            }

            return new ParallelOperation(true, this);
        }

        void OperationStarted()
        {
            Interlocked.Increment(ref _currentParallelOperationsCount);
        }

        void OperationFinished()
        {
            Interlocked.Decrement(ref _currentParallelOperationsCount);
            _semaphore.Release(1);
        }

        /// <summary>
        /// Gets a disposable token for the parallel operation - the token indicates whether it's ok to continue
        /// </summary>
        public class ParallelOperation : IDisposable
        {
            readonly bool _canContinue;
            readonly ParallelOperationsManager _parallelOperationsManager;

            internal ParallelOperation(bool canContinue, ParallelOperationsManager parallelOperationsManager)
            {
                _canContinue = canContinue;
                _parallelOperationsManager = parallelOperationsManager;

                if (!_canContinue) return;

                _parallelOperationsManager.OperationStarted();
            }

            /// <summary>
            /// Ends this parallel operation
            /// </summary>
            public void Dispose()
            {
                if (!_canContinue) return;

                _parallelOperationsManager.OperationFinished();
            }

            /// <summary>
            /// Gets whether the token was successfully acquired
            /// </summary>
            public bool CanContinue()
            {
                return _canContinue;
            }
        }
    }
}