using System.Collections.Concurrent;
using EventStore.ClientAPI;
using Rebus.Bus;
using System;

namespace Rebus.EventStore
{
    // TODO: Needs to be thread safe?
    public class EventStoreMessageQueue : IMulticastTransport, IDisposable, INeedInitializationBeforeStart
    {
        IEventStoreConnection connection;
        readonly string applicationId;
        EventStoreReceiveMessages receiveMessages;
        public string InputQueue { get; private set; }
        public string InputQueueAddress { get; private set; }
        public bool ManagesSubscriptions { get; private set; }
        readonly ConcurrentDictionary<Type, EventStoreReceiveMessages> subscriptions = new ConcurrentDictionary<Type, EventStoreReceiveMessages>();


        public EventStoreMessageQueue(string applicationId, string inputQueue)
        {
            if (applicationId == null) throw new ArgumentNullException("applicationId");

            InputQueue = new EventStoreQueueIdentifier(inputQueue).StreamId;
            InputQueueAddress = new EventStoreQueueIdentifier(inputQueue).StreamId;

            this.applicationId = applicationId;
            ManagesSubscriptions = true;
        }

        public void Send(string destination, TransportMessageToSend message, ITransactionContext context)
        {
            new EventStoreSendMessages(connection).Send(destination, message, context);
        }

        public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
        {
            var fromInputQueue = receiveMessages.ReceiveMessage(context);

            if (fromInputQueue != null) return fromInputQueue;

            foreach (var eventStoreReceiveMessages in subscriptions.Values)
            {
                var fromSubscription = eventStoreReceiveMessages.ReceiveMessage(context);

                if (fromSubscription != null) return fromSubscription;
            }

            return null;
        }

        public void Subscribe(Type eventType, string inputQueueAddress)
        {
            var inputQueue = GetEventName(eventType);
            subscriptions[eventType] = new EventStoreReceiveMessages(applicationId, inputQueue, connection);
        }

        public void Unsubscribe(Type messageType, string inputQueueAddress)
        {
            subscriptions[messageType].Dispose();
        }

        public string GetEventName(Type messageType)
        {
            return messageType.ToString();
        }

        public void Dispose()
        {
            if (connection != null) connection.Dispose();
            if (receiveMessages != null) receiveMessages.Dispose();
        }

        public void Initialize()
        {
            connection = EventStoreConnectionManager.CreateConnectionAndWait();

            receiveMessages = new EventStoreReceiveMessages(applicationId, InputQueue, connection);
        }
    }
}
