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
        /// Constructs the context and explicitly does NOT set itself as current in <see cref="TransactionContext"/>, because that would not make sense...
        /// i.e. when would the "context" end when there's no transaction? The answer is that the <see cref="NoTransaction"/> implementation of
        /// <see cref="ITransactionContext"/> must never be set as the current transaction context, it must always be constructed when it must be used
        /// </summary>
        public NoTransaction()
        {
            //TransactionContext.Set(this);
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