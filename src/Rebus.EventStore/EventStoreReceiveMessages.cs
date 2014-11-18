using System;
using System.Collections.Concurrent;
using System.Text;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Newtonsoft.Json;

namespace Rebus.EventStore
{
    public class EventStoreReceiveMessages : IReceiveMessages, IDisposable
    {
        readonly IEventStoreConnection connection;
        readonly ConcurrentQueue<MessageContainer> receivedMessages = new ConcurrentQueue<MessageContainer>();
        EventStorePersistentSubscription subscription;
        public string InputQueue { get; private set; }
        public string InputQueueAddress { get; private set; }
        string applicationId;

        public EventStoreReceiveMessages(string applicationId, string inputQueue, IEventStoreConnection connection)
        {
            if (applicationId == null) throw new ArgumentNullException("applicationId");
            if (inputQueue == null) throw new ArgumentNullException("inputQueue");
            if (connection == null) throw new ArgumentNullException("connection");

            this.applicationId = applicationId;
            this.connection = connection;
            InputQueue = inputQueue;
            InputQueueAddress = inputQueue;

            SubscribeToInputQueue();
        }

        private void SubscribeToInputQueue()
        {
            var settings = PersistentSubscriptionSettingsBuilder.Create()
                .StartFromCurrent()
                .PreferDispatchToSingle()
                .WithMaxRetriesOf(5)
                .Build();

            try
            {
                var result = connection.CreatePersistentSubscriptionAsync(InputQueue, applicationId, settings,
                    new UserCredentials("admin", "changeit")).Result;
            }
            catch (AggregateException)
            {
                // TODO: inspect and make sure its the right exception..
            }
           
            subscription = connection.ConnectToPersistentSubscription(applicationId, InputQueue, EventAppeared, autoAck: false);
        }

        private void EventAppeared(EventStorePersistentSubscription subscription, ResolvedEvent evt)
        {
            var originalTransportMessage = JsonConvert.DeserializeObject<TransportMessageToSend>(Encoding.UTF8.GetString(evt.OriginalEvent.Data));
            var message = new ReceivedTransportMessage {Id = evt.OriginalEvent.EventId.ToString("N"), Body = originalTransportMessage.Body, Headers = originalTransportMessage.Headers, Label = originalTransportMessage.Label };
            receivedMessages.Enqueue(new MessageContainer { ReceivedTransportMessage = message, ResolvedEvent = evt });
        }

        public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
        {
            if (receivedMessages.IsEmpty)
            {
                return null;
            }

            MessageContainer message;
            if (receivedMessages.TryDequeue(out message) == false)
            {
                return null;
            }

            // Leaking handlers?
            if (context.IsTransactional)
            {
                context.DoCommit += () => subscription.Acknowledge(message.ResolvedEvent);

                context.DoRollback += () => subscription.Fail(message.ResolvedEvent, PersistentSubscriptionNakEventAction.Retry, "Rebus transaction rolled back..");
            }

            return message.ReceivedTransportMessage;
        }

        private class MessageContainer
        {
            public ReceivedTransportMessage ReceivedTransportMessage { get; set; }
            public ResolvedEvent ResolvedEvent { get; set; }
        }

        public void Dispose()
        {
            if(subscription != null) subscription.Stop(TimeSpan.FromSeconds(2));
        }
    }
}