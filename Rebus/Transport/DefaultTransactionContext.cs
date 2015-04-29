using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rebus.Transport
{
    public class DefaultTransactionContext : ITransactionContext
    {
        readonly List<Func<Task>> _onCommittedActions = new List<Func<Task>>();
        
        readonly List<Action> _onAbortedActions = new List<Action>();
        readonly List<Action> _onDisposedActions = new List<Action>();

        bool _mustAbort;
        bool _completed;
        bool _aborted;
        bool _cleanedUp;

        public DefaultTransactionContext()
        {
            Items = new Dictionary<string, object>();
        }

        public Dictionary<string, object> Items { get; private set; }

        public void OnCommitted(Func<Task> commitAction)
        {
            _onCommittedActions.Add(commitAction);
        }

        public void OnAborted(Action abortedAction)
        {
            _onAbortedActions.Add(abortedAction);
        }

        public void OnDisposed(Action disposedAction)
        {
            _onDisposedActions.Add(disposedAction);
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