using System.Runtime.Remoting.Messaging;

namespace Rebus.Transport
{
    /// <summary>
    /// Provides an "ambient" context for stashing away an instance that implements the current <see cref="ITransactionContext"/>. The
    /// ambient transaction context is automatically preserved even though threads are changed etc.
    /// </summary>
    public static class AmbientTransactionContext
    {
        const string TransactionContextKey = "rebus2-current-transaction-context";

        /// <summary>
        /// Gets/sets the current transaction context from the call context's logical data slot (which is automatically transferred to continuations when resuming
        /// awaited calls)
        /// </summary>
        public static ITransactionContext Current
        {
            get
            {
                return CallContext.LogicalGetData(TransactionContextKey) as ITransactionContext;
            }
            set
            {
                CallContext.LogicalSetData(TransactionContextKey, value);
            }
        }
    }
}