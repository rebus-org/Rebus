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

            if (context.IsTransactional && TransactionAlreadyStarted(context))
            {
                WriteInTransactionAndWait(message, CurrentTransaction(context));
            }
            else if (context.IsTransactional && TransactionIsFresh(context))
            {
                var transaction = StartNewEventStoreTransaction(destination);
                
                SetCurrentTransaction(context, transaction);
                
                SetupTransactionEventHandlers(context, transaction);

                WriteInTransactionAndWait(message, transaction);
            }
            else
            {
                WriteAndWait(destination, message);
            }
        }

        private void SetupTransactionEventHandlers(ITransactionContext context, EventStoreTransaction transaction)
        {
            context.DoCommit += () => transaction.CommitAsync().Wait();
            context.DoRollback += transaction.Rollback;
            context.AfterRollback += () => SetCurrentTransaction(context, null);
        }

        private EventStoreTransaction StartNewEventStoreTransaction(string destination)
        {
            return connection.StartTransactionAsync(destination, ExpectedVersion.Any).Result;
        }

        private void WriteInTransactionAndWait(TransportMessageToSend message, EventStoreTransaction transaction)
        {
            transaction.WriteAsync(CreateMessage(message)).Wait();
        }

        void SetCurrentTransaction(ITransactionContext context, EventStoreTransaction transaction)
        {
            if (TransactionIsFresh(context) == false) throw new InvalidOperationException("Overriding existing transaction!");

            context["singleTransaction"] = transaction;
        }

        bool TransactionIsFresh(ITransactionContext context)
        {
            return context["singleTransaction"] == null;
        }

        bool TransactionAlreadyStarted(ITransactionContext context)
        {
            return context["singleTransaction"] != null;
        }

        EventStoreTransaction CurrentTransaction(ITransactionContext context)
        {
            if(TransactionAlreadyStarted(context) == false) throw new InvalidOperationException("No existing transaction!");

            return context["singleTransaction"] as EventStoreTransaction;
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