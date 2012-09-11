using System;
using System.Collections.Generic;
using System.Transactions;

namespace Rebus.Bus
{
    /// <summary>
    /// Implementation of <see cref="ITransactionContext"/> that is tied to an ambient .NET transaction.
    /// </summary>
    public class AmbientTransactionContext : IEnlistmentNotification, ITransactionContext
    {
        readonly Dictionary<string, object> items = new Dictionary<string, object>();

        /// <summary>
        /// Constructs the context, enlists it in the ambient transaction, and sets itself as the current context in <see cref="TransactionContext"/>.
        /// </summary>
        public AmbientTransactionContext()
        {
            if (Transaction.Current == null)
            {
                throw new InvalidOperationException("There's currently no ambient transaction associated with this thread." +
                                                    " You can only instantiate this class within a TransactionScope.");
            }
            Transaction.Current.EnlistVolatile(this, EnlistmentOptions.None);
            TransactionContext.Set(this);
        }

        public event Action DoCommit = delegate { };
        
        public event Action BeforeCommit = delegate { };
        
        public event Action DoRollback = delegate { };
        
        public event Action AfterRollback = delegate { };

        public bool IsTransactional { get { return true; } }

        public object this[string key]
        {
            get { return items.ContainsKey(key) ? items[key] : null; }
            set { items[key] = value; }
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            BeforeCommit();
            DoCommit();
            TransactionContext.Clear();
            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            DoRollback();
            enlistment.Done();
            TransactionContext.Clear();
            AfterRollback();
        }

        public void InDoubt(Enlistment enlistment)
        {
            enlistment.Done();
        }
    }
}