using System.Collections.Concurrent;
using EventStore.ClientAPI;
using Rebus.Bus;
using System;

namespace Rebus.EventStore
{
    // TODO: Needs to be thread safe? it must be reentrant regarding send and receive..! see mogens commment on github..
    public class EventStoreMessageQueue : IMulticastTransport, IDisposable, INeedInitializationBeforeStart
    {
        IEventStoreConnection connection;
        readonly string applicationId;
        EventStoreReceiveMessages receiveMessagesFromInputQueue;
        private EventStoreSendMessages sendMessages;
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
            sendMessages.Send(destination, message, context);
        }

        public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
        {
            var fromInputQueue = receiveMessagesFromInputQueue.ReceiveMessage(context);

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
            subscriptions[eventType] = new EventStoreReceiveMessages(connection, applicationId, inputQueue);
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
            if (connection != null)
            {
                connection.Dispose();
            }
            if (receiveMessagesFromInputQueue != null)
            {
                receiveMessagesFromInputQueue.Dispose();
            }
        }

        // TODO: make thread safe?
        public void Initialize()
        {
            if (connection == null)
            {
                connection = EventStoreConnectionManager.CreateConnectionAndWait();
            }

            if (receiveMessagesFromInputQueue == null)
            {
                receiveMessagesFromInputQueue = new EventStoreReceiveMessages(connection, applicationId, InputQueue);
            }

            if (sendMessages == null)
            {
                sendMessages = new EventStoreSendMessages(connection);
            }
        }
    }
}
