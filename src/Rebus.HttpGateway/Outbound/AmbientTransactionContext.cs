using System;
using System.Collections.Generic;
using System.Transactions;
using Rebus.Transports.Msmq;

namespace Rebus.HttpGateway.Outbound
{
    class AmbientTransactionContext : IEnlistmentNotification, ITransactionContext
    {
        public static AmbientTransactionContext NewAmbientContext()
        {
            var bomkarl = new AmbientTransactionContext();
            Transaction.Current.EnlistVolatile(bomkarl, EnlistmentOptions.None);
            return bomkarl;
        }

        [ThreadStatic]
        internal static AmbientTransactionContext CurrentBomkarl;

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
            CurrentBomkarl = null;
            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            RaiseDoRollback();
            CurrentBomkarl = null;
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