using System.Runtime.Remoting.Messaging;

namespace Rebus.Bus
{
    /// <summary>
    /// Gives access to a thread-bound <see cref="ITransactionContext"/>
    /// </summary>
    public class TransactionContext
    {
        const string TransactionContextKey = "rebus-transaction-context";

        /// <summary>
        /// Gets the <see cref="ITransactionContext"/> associated with the current thread
        /// </summary>
        public static ITransactionContext Current
        {
            get
            {
                if (RebusHttpContext.InContext)
                    return RebusHttpContext.TransactionContext;
                if (RebusOperationContext.InContext)
                    return RebusOperationContext.TransactionContext;
                return CallContext.LogicalGetData(TransactionContextKey) as ITransactionContext;
            }
        }

        /// <summary>
        /// Assigns the specified <see cref="ITransactionContext"/> to the current context
        /// </summary>
        public static void Set(ITransactionContext context)
        {
            if (RebusHttpContext.InContext)
                RebusHttpContext.TransactionContext = context;
            else if (RebusOperationContext.InContext)
                RebusOperationContext.TransactionContext = context;
            else
                CallContext.LogicalSetData(TransactionContextKey, context);
        }

        /// <summary>
        /// Clears any context that might currently be assigned
        /// </summary>
        public static void Clear()
        {
            RebusHttpContext.Clear();
            RebusOperationContext.Clear();
            CallContext.FreeNamedDataSlot(TransactionContextKey);
        }

        internal static ITransactionContext None()
        {
            return new NoTransaction();
        }
    }
}