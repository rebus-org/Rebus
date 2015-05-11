using System;

namespace Rebus.Threading
{
    /// <summary>
    /// Helper that counts the number of parallel operations. Not reentrant, this bad boy is meant to be used from a single worker thread
    /// that may use it to count the number of async parallel operations waiting to be completed
    /// </summary>
    public class ParallelismCounter
    {
        readonly int _maxParallelism;
        int _currentParallelism;

        /// <summary>
        /// Constructs the container with the given max number of parallel async operations to allow
        /// </summary>
        public ParallelismCounter(int maxParallelism)
        {
            _maxParallelism = Math.Max(1, maxParallelism);
        }

        /// <summary>
        /// Returns whether it's OK to start yet another parallel operation
        /// </summary>
        public bool CanContinue()
        {
            return _currentParallelism < _maxParallelism;
        }

        /// <summary>
        /// Begins another async operation and returns an <see cref="IDisposable"/> that must be disposed in order to mark the end of the async operation
        /// </summary>
        public IDisposable Begin()
        {
            _currentParallelism++;
            return new ParallismCounterDecreaser(() => _currentParallelism--);
        }

        class ParallismCounterDecreaser : IDisposable
        {
            readonly Action _disposeAction;

            public ParallismCounterDecreaser(Action disposeAction)
            {
                _disposeAction = disposeAction;
            }

            public void Dispose()
            {
                _disposeAction();
            }
        }
    }
}