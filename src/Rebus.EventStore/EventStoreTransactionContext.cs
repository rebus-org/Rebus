using System;
using EventStore.ClientAPI;

namespace Rebus.EventStore
{
    internal class EventStoreTransactionContext
    {
        public void SetCurrentTransaction(ITransactionContext context, EventStoreTransaction transaction)
        {
            if (TransactionIsFresh(context) == false) throw new InvalidOperationException("Overriding existing transaction!");

            context["singleTransaction"] = transaction;
        }

        public bool TransactionIsFresh(ITransactionContext context)
        {
            return context["singleTransaction"] == null;
        }

        public bool TransactionAlreadyStarted(ITransactionContext context)
        {
            return context["singleTransaction"] != null;
        }

        public EventStoreTransaction CurrentTransaction(ITransactionContext context)
        {
            if (TransactionAlreadyStarted(context) == false) throw new InvalidOperationException("No existing transaction!");

            return context["singleTransaction"] as EventStoreTransaction;
        }

    }
}