using System;
using System.Collections.Generic;

namespace Rebus.Bus
{
    /// <summary>
    /// Transaction context that really means "no transaction". Sort of a null object implementation
    /// of a transaction context.
    /// </summary>
    public class NoTransaction : ITransactionContext
    {
        readonly Dictionary<string, object> items = new Dictionary<string, object>();

        /// <summary>
        /// Return false beause the <see cref="NoTransaction"/> implementation of <see cref="ITransactionContext"/> is always non-transactional
        /// </summary>
        public bool IsTransactional { get { return false; } }

        /// <summary>
        /// Constructs the context and sets itself as current in <see cref="TransactionContext"/>.
        /// It must be disposed immediately after use.
        /// </summary>
        public NoTransaction()
        {
            TransactionContext.Set(this);
        }

        /// <summary>
        /// Formats itself as 'no transaction'
        /// </summary>
        public override string ToString()
        {
            return "no tx";
        }

        /// <summary>
        /// Gives access to a dictionary of stuff that will be kept for the duration of the transaction.
        /// </summary>
        public object this[string key]
        {
            get { return items.ContainsKey(key) ? items[key] : null; }
            set { items[key] = value; }
        }

        /// <summary>
        /// Event that is never raised because this implementation of <see cref="ITransactionContext"/> is no transaction.
        /// Will throw an <see cref="InvalidOperationException"/> if someone subscribes
        /// </summary>
        public event Action DoCommit
        {
            add { throw new InvalidOperationException("Don't add commit/rollback events when you're nontransactional"); }
            remove{}
        }

        /// <summary>
        /// Event that is never raised because this implementation of <see cref="ITransactionContext"/> is no transaction.
        /// Will throw an <see cref="InvalidOperationException"/> if someone subscribes
        /// </summary>
        public event Action DoRollback
        {
            add{ throw new InvalidOperationException("Don't add commit/rollback events when you're nontransactional");}
            remove{}
        }

        /// <summary>
        /// Event that is never raised because this implementation of <see cref="ITransactionContext"/> is no transaction.
        /// Will throw an <see cref="InvalidOperationException"/> if someone subscribes
        /// </summary>
        public event Action BeforeCommit
        {
            add { throw new InvalidOperationException("Don't add commit/rollback events when you're nontransactional"); }
            remove{}
        }

        /// <summary>
        /// Event that is never raised because this implementation of <see cref="ITransactionContext"/> is no transaction.
        /// Will throw an <see cref="InvalidOperationException"/> if someone subscribes
        /// </summary>
        public event Action AfterRollback
        {
            add{ throw new InvalidOperationException("Don't add commit/rollback events when you're nontransactional");}
            remove{}
        }

        /// <summary>
        /// Event that is raised when this transaction context is disposed
        /// </summary>
        public event Action Cleanup = delegate { };

        /// <summary>
        /// Detaches this transaction context from the thread
        /// </summary>
        public void Dispose()
        {
            TransactionContext.Clear();
            Cleanup();
        }
    }
}