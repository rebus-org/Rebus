using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rebus.Transport
{
    public class DefaultTransactionContext : ITransactionContext
    {
        readonly List<Func<Task>> _onCommittedActions = new List<Func<Task>>();
        readonly List<Func<Task>> _onAbortedActions = new List<Func<Task>>();
        readonly List<Func<Task>> _onDisposedActions = new List<Func<Task>>();

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

        public void OnAborted(Func<Task> abortedAction)
        {
            _onAbortedActions.Add(abortedAction);
        }

        public void OnDisposed(Func<Task> disposedAction)
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

        public async Task CleanUp()
        {
            if (_cleanedUp) return;

            try
            {
                if (!_completed)
                {
                    await RaiseAborted();
                }

                await Invoke(_onDisposedActions);
            }
            finally
            {
                _cleanedUp = true;
            }
        }

        public void Dispose()
        {
            if (!_cleanedUp)
            {
                throw new InvalidOperationException("DefaultTransactionContext was disposed without being cleaned up!!");
            }
        }

        /// <summary>
        /// Ends the current transaction but either committing it or aborting it, depending on whether someone voted for abortion
        /// </summary>
        public async Task Complete()
        {
            if (_mustAbort)
            {
                await RaiseAborted();
                return;
            }

            await RaiseCommitted();
        }

        async Task RaiseAborted()
        {
            if (_aborted) return;
            await Invoke(_onAbortedActions);
            _aborted = true;
        }

        async Task RaiseCommitted()
        {
            await Invoke(_onCommittedActions);
            _completed = true;
        }

        static async Task Invoke(IEnumerable<Func<Task>> actions)
        {
            await Task.WhenAll(actions.Select(a => a()).ToArray());
        }
    }
}