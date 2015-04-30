using System;

namespace Rebus.Threading
{
    public class ParallelismCounter
    {
        readonly int _maxParallelism;
        int _currentParallelism;

        public ParallelismCounter(int maxParallelism)
        {
            _maxParallelism = Math.Max(1, maxParallelism);
        }

        public bool CanContinue()
        {
            return _currentParallelism < _maxParallelism;
        }

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