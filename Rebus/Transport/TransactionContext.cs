using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Rebus.Transport
{
    class TransactionContext : ITransactionContext
    {
        readonly ConcurrentQueue<Func<Task>> _onCommittedActions = new ConcurrentQueue<Func<Task>>();
        readonly ConcurrentQueue<Func<Task>> _onCompletedActions = new ConcurrentQueue<Func<Task>>();

        readonly ConcurrentQueue<Action> _onAbortedActions = new ConcurrentQueue<Action>();
        readonly ConcurrentQueue<Action> _onDisposedActions = new ConcurrentQueue<Action>();

        bool _mustAbort;
        bool _completed;
        bool _aborted;
        bool _cleanedUp;
        bool _disposed;

        public ConcurrentDictionary<string, object> Items { get; } = new ConcurrentDictionary<string, object>();

        public void OnCommitted(Func<Task> commitAction) => _onCommittedActions.Enqueue(commitAction);

        public void OnCompleted(Func<Task> completedAction) => _onCompletedActions.Enqueue(completedAction);

        public void OnAborted(Action abortedAction) => _onAbortedActions.Enqueue(abortedAction);

        public void OnDisposed(Action disposedAction) => _onDisposedActions.Enqueue(disposedAction);

        public void Abort() => _mustAbort = true;

        public async Task Commit() => await Invoke(_onCommittedActions).ConfigureAwait(false);

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
                        Invoke(_onDisposedActions);
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

            await RaiseCommitted().ConfigureAwait(false);

            await RaiseCompleted().ConfigureAwait(false);

            Dispose();
        }

        void RaiseAborted()
        {
            if (_aborted) return;
            Invoke(_onAbortedActions);
            _aborted = true;
        }

        async Task RaiseCommitted() => await Invoke(_onCommittedActions).ConfigureAwait(false);

        async Task RaiseCompleted()
        {
            await Invoke(_onCompletedActions).ConfigureAwait(false);
            _completed = true;
        }

        static void Invoke(ConcurrentQueue<Action> actions)
        {
            while (actions.TryDequeue(out var action))
            {
                action();
            }
        }

        static async Task Invoke(ConcurrentQueue<Func<Task>> actions)
        {
            while (actions.TryDequeue(out var action))
            {
                await action().ConfigureAwait(false);
            }
        }
    }
}