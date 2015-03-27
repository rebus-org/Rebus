using System;
using System.Collections.Generic;

namespace Rebus.Transport
{
    public class DefaultTransactionContext : ITransactionContext
    {
        bool _mustAbort;
        bool _completed;
        bool _aborted;

        public DefaultTransactionContext()
        {
            Items = new Dictionary<string, object>();
        }

        public Dictionary<string, object> Items { get; private set; }
        
        public event Action Committed;
        
        public event Action Aborted;

        public event Action Cleanup;

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
                var cleanup = Cleanup;
                if (cleanup != null) cleanup();
            }
        }

        void RaiseAborted()
        {
            if (_aborted) return;
            var aborted = Aborted;
            if (aborted != null) aborted();
            _aborted = true;
        }

        /// <summary>
        /// Ends the current transaction but either committing it or aborting it, depending on whether someone voted for abortion
        /// </summary>
        public void Complete()
        {
            if (_mustAbort)
            {
                RaiseAborted();
                return;
            }

            var committed = Committed;
            if (committed != null) committed();

            _completed = true;
        }
    }
}