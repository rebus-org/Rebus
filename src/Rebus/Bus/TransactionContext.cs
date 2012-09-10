using System;

namespace Rebus.Bus
{
    /// <summary>
    /// Gives access to a thread-bound <see cref="ITransactionContext"/>
    /// </summary>
    public class TransactionContext
    {
        [ThreadStatic] static ITransactionContext current;

        public static ITransactionContext Current
        {
            get { return current; }
        }

        public static void Set(ITransactionContext context)
        {
            current = context;
        }

        public static void Clear()
        {
            current = null;
        }
    }
}