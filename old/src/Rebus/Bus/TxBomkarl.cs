using System;
using System.Collections.Generic;
using System.Threading;

namespace Rebus.Bus
{
    /// <summary>
    /// Special implementation of <see cref="ITransactionContext"/> that is designed to work with message handlers
    /// </summary>
    class TxBomkarl : ITransactionContext
    {
        readonly Dictionary<string, object> items = new Dictionary<string, object>();
        readonly string threadName;

        public event Action DoCommit = delegate { };
        
        public event Action BeforeCommit = delegate { };
        
        public event Action DoRollback = delegate { };
        
        public event Action AfterRollback = delegate { };

        public event Action Cleanup = delegate { };

        /// <summary>
        /// Constructs the context and sets itself as current in <see cref="TransactionContext"/>.
        /// </summary>
        public TxBomkarl()
        {
            TransactionContext.Set(this);
            threadName = Thread.CurrentThread.Name;
        }

        public override string ToString()
        {
            return string.Format("handler tx on thread '{0}'", threadName);
        }

        public bool IsTransactional { get { return true; } }

        /// <summary>
        /// Gives access to a dictionary of stuff that will be kept for the duration of the transaction.
        /// </summary>
        public object this[string key]
        {
            get { return items.ContainsKey(key) ? items[key] : null; }
            set { items[key] = value; }
        }

        public void RaiseDoCommit()
        {
            BeforeCommit();

            DoCommit();
        }

        public void RaiseDoRollback()
        {
            DoRollback();

            AfterRollback();
        }

        public void Dispose()
        {
            TransactionContext.Clear();
            Cleanup();
        }
    }
}