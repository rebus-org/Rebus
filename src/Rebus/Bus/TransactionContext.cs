using System;

namespace Rebus.Bus
{
    /// <summary>
    /// Gives access to a thread-bound <see cref="ITransactionContext"/>
    /// </summary>
    public class TransactionContext
    {
        [ThreadStatic] static ITransactionContext current;

        /// <summary>
        /// Gets the <see cref="ITransactionContext"/> associated with the current thread
        /// </summary>
        public static ITransactionContext Current
        {
            get { return current; }
        }

        /// <summary>
        /// Assigns the specified <see cref="ITransactionContext"/> to the current thread
        /// </summary>
        public static void Set(ITransactionContext context)
        {
            current = context;
        }

        /// <summary>
        /// Clears any context that might currently be assigned to the current thread
        /// </summary>
        public static void Clear()
        {
            current = null;
        }
    }
}