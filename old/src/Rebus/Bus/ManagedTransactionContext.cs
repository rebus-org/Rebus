using System;
using System.Transactions;

namespace Rebus.Bus
{
    internal class ManagedTransactionContext : IDisposable
    {
        public ITransactionContext Context { get; private set; }

        ManagedTransactionContext(ITransactionContext context)
        {
            Context = context;
        }

        public static ManagedTransactionContext Get()
        {
            if (TransactionContext.Current != null)
                return new ManagedTransactionContext(TransactionContext.Current);

            if (Transaction.Current != null)
                return new ManagedTransactionContext(new AmbientTransactionContext());

            return new ManagedTransactionContext(new NoTransaction());
        }

        public void Dispose()
        {
            if (Context is NoTransaction)
            {
                Context.Dispose();
            }
        }
    }
}