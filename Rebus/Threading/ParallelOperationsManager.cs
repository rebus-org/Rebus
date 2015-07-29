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
        long _currentParallelism;

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
        public bool HasPendingTasks
        {
            get { return Interlocked.Read(ref _currentParallelism) > 0; }
        }

        /// <summary>
        /// Begins another async operation and returns an <see cref="IDisposable"/> that must be disposed in order to mark the end of the async operation
        /// </summary>
        public ParallelOperation TryBegin()
        {
            if (!_semaphore.Wait(TimeSpan.Zero))
            {
                return new ParallelOperation(() => { }, false);
            }
            
            Interlocked.Increment(ref _currentParallelism);

            return new ParallelOperation(() =>
            {
                Interlocked.Decrement(ref _currentParallelism);
                _semaphore.Release();
            }, true);
        }

        /// <summary>
        /// Gets a disposable token for the parallel operation - the token indicates whether it's ok to continue
        /// </summary>
        public class ParallelOperation : IDisposable
        {
            readonly Action _disposeAction;
            readonly bool _canContinue;

            internal ParallelOperation(Action disposeAction, bool canContinue)
            {
                _disposeAction = disposeAction;
                _canContinue = canContinue;
            }

            public void Dispose()
            {
                _disposeAction();
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