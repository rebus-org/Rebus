using System.Threading.Tasks;
using EventStore.ClientAPI;
using Newtonsoft.Json;
using System;
using System.Text;

namespace Rebus.EventStore
{
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
                WriteInTransactionAsync(message, CurrentEventStoreTransactionInContext(context));
            }
            else if (context.IsTransactional && TransactionContextIsFresh(context))
            {
                StartNewEventStoreTransaction(destination, message, context);
            }
            else
            {
                Write(destination, message);
            }
        }

        async void StartNewEventStoreTransaction(string destination, TransportMessageToSend message, ITransactionContext context)
        {
            var transaction = await connection.StartTransactionAsync(destination, ExpectedVersion.Any);

            InitializeTransactionContext(context, transaction);

            RegisterTransactionEventHandlers(context, transaction);

            WriteInTransactionAsync(message, transaction);
        }

        private void RegisterTransactionEventHandlers(ITransactionContext context, EventStoreTransaction transaction)
        {
            context.DoCommit += () => transaction.CommitAsync().Wait();
            context.DoRollback += transaction.Rollback;
            context.AfterRollback += () => InitializeTransactionContext(context, null);
        }

        private void WriteInTransactionAsync(TransportMessageToSend message, EventStoreTransaction transaction)
        {
            transaction.WriteAsync(CreateMessage(message));
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

        void Write(string destination, TransportMessageToSend message)
        {
            connection.AppendToStreamAsync(destination, ExpectedVersion.Any, CreateMessage(message));
        }

        EventData CreateMessage(TransportMessageToSend message)
        {
            var messageAsJson = Encoding.UTF8.GetBytes((string)JsonConvert.SerializeObject(message));
            return new EventData(Guid.NewGuid(), message.GetType().ToString(), true, messageAsJson, null);
        }
    }
}