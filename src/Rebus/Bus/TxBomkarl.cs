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

        public TxBomkarl()
        {
            TransactionContext.Set(this);
        }

        public void RaiseDoCommit()
        {
            Console.WriteLine("Before commit!");
            BeforeCommit();

            Console.WriteLine("Commit!");
            DoCommit();
        }

        public void RaiseDoRollback()
        {
            Console.WriteLine("Rollback!");
            DoRollback();

            Console.WriteLine("After rollback!");
            AfterRollback();
        }

        public bool IsTransactional { get { return true; } }

        public object this[string key]
        {
            get { return items.ContainsKey(key) ? items[key] : null; }
            set { items[key] = value; }
        }

        public void Dispose()
        {
            TransactionContext.Clear();
        }
    }
}