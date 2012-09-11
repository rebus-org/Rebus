using System;
using System.Collections.Generic;

namespace Rebus.Bus
{
    class TxBomkarl : ITransactionContext, IDisposable
    {
        readonly Dictionary<string, object> items = new Dictionary<string, object>();

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
        }

        public bool IsTransactional { get { return true; } }

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