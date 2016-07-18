using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rebus.Transport
{
    /// <summary>
    /// Default implementation of <see cref="ITransactionContext"/>
    /// </summary>
    public class DefaultTransactionContext : ITransactionContext
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

        /// <summary>
        /// Stash of items that can carry stuff for later use in the transaction
        /// </summary>
        public ConcurrentDictionary<string, object> Items { get; } = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// Registers a listener to be called when the queue transaction is committed. This hook is reserved for the queue transaction
        /// and you may get unpredictable results of you enlist your own transaction in this
        /// </summary>
        public void OnCommitted(Func<Task> commitAction)
        {
            _onCommittedActions.Enqueue(commitAction);
        }

        /// <summary>
        /// Registers a listener to be called AFTER the queue transaction has been successfully committed (i.e. all listeners
        /// registered with <see cref="ITransactionContext.OnCommitted"/> have been executed). This would be a good place to complete the incoming
        /// message.
        /// </summary>
        public void OnCompleted(Func<Task> completedAction)
        {
            _onCompletedActions.Enqueue(completedAction);
        }

        /// <summary>
        /// Registers a listener to be called when the queue transaction is aborted. This hook is reserved for the queue transaction
        /// and you may get unpredictable results of you enlist your own transaction in this
        /// </summary>
        public void OnAborted(Action abortedAction)
        {
            _onAbortedActions.Enqueue(abortedAction);
        }

        /// <summary>
        /// Registers a listener to be called after the transaction is over
        /// </summary>
        public void OnDisposed(Action disposedAction)
        {
            _onDisposedActions.Enqueue(disposedAction);
        }

        /// <summary>
        /// Indicates that the transaction must not be committed and commit handlers must not be run
        /// </summary>
        public void Abort()
        {
            _mustAbort = true;
        }

        /// <summary>
        /// Executes commit actions enlisted in the transaction with <see cref="ITransactionContext.OnCommitted"/>
        /// </summary>
        public async Task Commit()
        {
            await Invoke(_onCommittedActions);
        }

        /// <summary>
        /// Performs the registered cleanup actions. If the transaction has not been committed, it will be aborted before the cleanup happens.
        /// </summary>
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

        /// <summary>
        /// Ends the current transaction by either committing it or aborting it, depending on whether someone voted for abortion
        /// </summary>
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

        void RaiseAborted()
        {
            if (_aborted) return;
            Invoke(_onAbortedActions);
            _aborted = true;
        }

        async Task RaiseCommitted()
        {
            await Invoke(_onCommittedActions);
        }

        async Task RaiseCompleted()
        {
            await Invoke(_onCompletedActions);
            _completed = true;
        }

        static void Invoke(ConcurrentQueue<Action> actions)
        {
            Action action;
            while (actions.TryDequeue(out action))
            {
                action();
            }
        }

        static async Task Invoke(ConcurrentQueue<Func<Task>> actions)
        {
            Func<Task> action;
            var actionsToExecuteNow = new List<Func<Task>>();
            while (actions.TryDequeue(out action))
            {
                actionsToExecuteNow.Add(action);
            }

            await Task.WhenAll(actionsToExecuteNow.Select(a => a()));
        }
    }
}