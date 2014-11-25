using EventStore.ClientAPI;
using Newtonsoft.Json;
using System;
using System.Text;

namespace Rebus.EventStore
{
    // TODO: rename *Wait* methods to *Synchrounous* or make async!

    public class EventStoreSendMessages : ISendMessages
    {
        readonly IEventStoreConnection connection;

        public EventStoreSendMessages(IEventStoreConnection connection)
        {
            this.connection = connection;
        }

        public void Send(string destination, TransportMessageToSend message, ITransactionContext context)
        {
            destination = new EventStoreQueueIdentifier(destination).StreamId;

            if (context.IsTransactional && TransactionContextIsFresh(context) == false)
            {
                WriteInTransactionAndWait(message, CurrentEventStoreTransactionInContext(context));
            }
            else if (context.IsTransactional && TransactionContextIsFresh(context))
            {
                var transaction = StartNewEventStoreTransaction(destination);
                
                InitializeTransactionContext(context, transaction);
                
                RegisterTransactionEventHandlers(context, transaction);

                WriteInTransactionAndWait(message, transaction);
            }
            else
            {
                WriteAndWait(destination, message);
            }
        }

        private void RegisterTransactionEventHandlers(ITransactionContext context, EventStoreTransaction transaction)
        {
            context.DoCommit += () => transaction.CommitAsync().Wait();
            context.DoRollback += transaction.Rollback;
            context.AfterRollback += () => InitializeTransactionContext(context, null);
        }

        private EventStoreTransaction StartNewEventStoreTransaction(string destination)
        {
            return connection.StartTransactionAsync(destination, ExpectedVersion.Any).Result;
        }

        private void WriteInTransactionAndWait(TransportMessageToSend message, EventStoreTransaction transaction)
        {
            transaction.WriteAsync(CreateMessage(message)).Wait();
        }

        void InitializeTransactionContext(ITransactionContext context, EventStoreTransaction transaction)
        {
            if (TransactionContextIsFresh(context) == false) throw new InvalidOperationException("Overriding existing transaction!");

            context["sendTransaction"] = transaction;
        }

        bool TransactionContextIsFresh(ITransactionContext context)
        {
            return context["sendTransaction"] == null;
        }

        EventStoreTransaction CurrentEventStoreTransactionInContext(ITransactionContext context)
        {
            if (TransactionContextIsFresh(context)) throw new InvalidOperationException("No existing transaction!");

            return context["sendTransaction"] as EventStoreTransaction;
        }

        void WriteAndWait(string destination, TransportMessageToSend message)
        {
            var result = connection.AppendToStreamAsync(destination, ExpectedVersion.Any, CreateMessage(message)).Result;
        }

        EventData CreateMessage(TransportMessageToSend message)
        {
            var messageAsJson = Encoding.UTF8.GetBytes((string)JsonConvert.SerializeObject(message));
            return new EventData(Guid.NewGuid(), message.GetType().ToString(), true, messageAsJson, null);
        }
    }
}