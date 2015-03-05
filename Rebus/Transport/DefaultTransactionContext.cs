using System;
using System.Collections.Generic;

namespace Rebus.Transport
{
    public class DefaultTransactionContext : ITransactionContext
    {
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
            _aborted = true;
        }

        public void Dispose()
        {
            var cleanup = Cleanup;
            if (cleanup != null) cleanup();
        }

        /// <summary>
        /// Ends the current transaction but either committing it or aborting it, depending on whether someone voted for abortion
        /// </summary>
        public void Complete()
        {
            if (_aborted)
            {
                var aborted = Aborted;
                if (aborted != null) aborted();
                return;
            }

            var committed = Committed;
            if (committed != null) committed();
        }
    }
}