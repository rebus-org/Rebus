using System;
using System.Collections.Generic;
using System.Transactions;

namespace Rebus.Bus
{
    public class AmbientTransactionContext : IEnlistmentNotification, ITransactionContext
    {
        public AmbientTransactionContext()
        {
            Transaction.Current.EnlistVolatile(this, EnlistmentOptions.None);
            TransactionContext.Set(this);
        }

        readonly Dictionary<string, object> items = new Dictionary<string, object>();

        public event Action DoCommit = delegate { };
        public event Action BeforeCommit = delegate { };
        public event Action DoRollback = delegate { };
        public event Action AfterRollback = delegate { };

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            BeforeCommit();
            RaiseDoCommit();
            TransactionContext.Clear();
            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            RaiseDoRollback();
            TransactionContext.Clear();
            enlistment.Done();
            AfterRollback();
        }

        public void InDoubt(Enlistment enlistment)
        {
            enlistment.Done();
        }

        public void RaiseDoCommit()
        {
            DoCommit();
        }

        public void RaiseDoRollback()
        {
            DoRollback();
        }

        public bool IsTransactional { get { return true; } }

        public object this[string key]
        {
            get { return items.ContainsKey(key) ? items[key] : null; }
            set { items[key] = value; }
        }
    }
}