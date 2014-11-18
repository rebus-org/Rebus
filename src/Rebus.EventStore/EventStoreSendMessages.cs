using System;
using System.Text;
using EventStore.ClientAPI;
using Newtonsoft.Json;

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
            var eventStoreQueue = new EventStoreQueueIdentifier(destination);

            if (destination == null) throw new ArgumentNullException("destination");
            if(destination.Contains("-")) throw new ArgumentException("destination stream cannot contain -");

            if (context.IsTransactional)
            {
                var transaction = connection.StartTransactionAsync(destination, ExpectedVersion.Any).Result;transaction.WriteAsync(CreateMessage(message)).Wait();

                context.DoCommit += () => transaction.CommitAsync().Wait();
                context.DoRollback += transaction.Rollback;
            }
            else
            {
                AppendToStream(destination, message);    
            }
        }

        private void AppendToStream(string destination, TransportMessageToSend message)
        {
            var result = connection.AppendToStreamAsync(destination, ExpectedVersion.Any, CreateMessage(message)).Result;
        }

        private EventData CreateMessage(TransportMessageToSend message)
        {
            var messageAsJson = Encoding.UTF8.GetBytes((string) JsonConvert.SerializeObject(message));
            return new EventData(Guid.NewGuid(), message.GetType().ToString(), true, messageAsJson, null);
        }
    }
}