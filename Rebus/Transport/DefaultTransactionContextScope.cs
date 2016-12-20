using System;
using System.Threading.Tasks;

namespace Rebus.Transport
{
    /// <summary>
    /// Default (async, as in: the <see cref="Complete"/> method returns a <see cref="Task"/> to be awaited) transaction scope 
    /// that sets up an ambient <see cref="ITransactionContext"/> and removes
    /// it when the scope is disposed. Call <code>await scope.Complete();</code> in order to end the scope
    /// by committing any actions enlisted to be executed.
    /// </summary>
    public class DefaultTransactionContextScope : IDisposable
    {
        readonly TransactionContext _transactionContext = new TransactionContext();

        /// <summary>
        /// Creates a new transaction context and mounts it on <see cref="AmbientTransactionContext.Current"/>, making it available for Rebus
        /// to pick up
        /// </summary>
        public DefaultTransactionContextScope()
        {
            AmbientTransactionContext.SetCurrent(_transactionContext);
        }

        /// <summary>
        /// Ends the current transaction by either committing it or aborting it, depending on whether someone voted for abortion
        /// </summary>
        public Task Complete()
        {
            return _transactionContext.Complete();
        }

        /// <summary>
        /// Disposes the transaction context and removes it from <see cref="AmbientTransactionContext.Current"/> again
        /// </summary>
        public void Dispose()
        {
            try
            {
                _transactionContext?.Dispose();
            }
            finally
            {
                AmbientTransactionContext.SetCurrent(null);
            }
        }
    }
}