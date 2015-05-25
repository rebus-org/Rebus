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

        /// <summary>
        /// Constructs the transaction context
        /// </summary>
        public DefaultTransactionContext()
        {
            Items = new Dictionary<string, object>();
        }

        public Dictionary<string, object> Items { get; private set; }

        public void OnCommitted(Func<Task> commitAction)
        {
            _onCommittedActions.Enqueue(commitAction);
        }

        public void OnCompleted(Func<Task> completedAction)
        {
            _onCompletedActions.Enqueue(completedAction);
        }

        public void OnAborted(Action abortedAction)
        {
            _onAbortedActions.Enqueue(abortedAction);
        }

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

        public void Dispose()
        {
            try
            {
                if (!_completed)
                {
                    RaiseAborted();
                }
            }
            finally
            {
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
        /// Ends the current transaction but either committing it or aborting it, depending on whether someone voted for abortion
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

        static void Invoke(IEnumerable<Action> actions)
        {
            foreach (var action in actions)
            {
                action();
            }    
        }

        static async Task Invoke(IEnumerable<Func<Task>> actions)
        {
            await Task.WhenAll(actions.Select(a => a()).ToArray());
        }
    }
}