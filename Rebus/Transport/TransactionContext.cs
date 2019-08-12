using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable SuggestBaseTypeForParameter

namespace Rebus.Transport
{
    class TransactionContext : ITransactionContext
    {
        // Note: C# generates thread-safe add/remove. They use a compare-and-exchange loop.
        event Func<Task> _onCommitted;
        event Func<Task> _onCompleted;        
        event Action _onAborted;
        event Action _onDisposed;

        bool _mustAbort;
        bool _completed;
        bool _aborted;
        bool _cleanedUp;
        bool _disposed;

        public ConcurrentDictionary<string, object> Items { get; } = new ConcurrentDictionary<string, object>();

        public void OnCommitted(Func<Task> commitAction)
        {
            if (_completed)
                ThrowCompletedException();

            _onCommitted += commitAction;
        }

        public void OnCompleted(Func<Task> completedAction)
        {
            if (_completed)
                ThrowCompletedException();

            _onCompleted += completedAction;
        }

        public void OnAborted(Action abortedAction)
        {
            if (_completed)
                ThrowCompletedException();

            _onAborted += abortedAction;
        }

        public void OnDisposed(Action disposedAction)
        {
            if (_completed)
                ThrowCompletedException();

            _onDisposed += disposedAction;
        }

        public void Abort() => _mustAbort = true;

        public Task Commit() => RaiseCommitted();

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                if (!_completed)
                {
                    RaiseAborted();
                }
            }
            finally
            {
                _disposed = true;

                if (!_cleanedUp)
                {
                    try
                    {
                        _onDisposed?.Invoke();
                    }
                    finally
                    {
                        _cleanedUp = true;
                    }
                }
            }
        }

        public async Task Complete()
        {
            if (_mustAbort)
            {
                RaiseAborted();
                return;
            }

            await RaiseCommitted();

            await RaiseCompleted();

            Dispose();
        }

        void ThrowCompletedException([CallerMemberName] string actionName = null)
        {
            throw new InvalidOperationException($"Cannot add {actionName} action on a completed transaction context.");
        }

        void RaiseAborted()
        {
            if (_aborted) return;
            _onAborted?.Invoke();
            _aborted = true;
        }

        Task RaiseCommitted() 
        {
            // RaiseCommitted() can be called multiple time.
            // So we atomically extract the current list of subscribers and reset the event to null (empty)
            var onCommitted = Interlocked.Exchange(ref _onCommitted, null);            
            return InvokeAsync(onCommitted);
        } 

        Task RaiseCompleted()
        {
            var task = InvokeAsync(_onCompleted);
            _completed = true;
            return task;
        }

        static async Task InvokeAsync(Func<Task> actions)
        {
            if (actions != null) 
            {    
                foreach (Func<Task> action in actions.GetInvocationList())
                {
                    await action();
                }
            }
        }
    }
}