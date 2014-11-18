using EventStore.ClientAPI;
using Rebus.EventStore;
using System;

namespace Rebus.Tests.Contracts.Transports.Factories
{
    public class EventStoreTransportFactory : ITransportFactory
    {
        readonly IEventStoreConnection eventStoreConnection;

        public EventStoreTransportFactory()
        {
            eventStoreConnection = EventStoreConnectionManager.CreateConnectionAndWait();
        }

        public Tuple<ISendMessages, IReceiveMessages> Create()
        {
            return Tuple.Create(CreateEventStoreSender(), CreateEventStoreReceiver(UniqueIdentifier("RebusTestInputQueue")));
        }

        ISendMessages CreateEventStoreSender()
        {
            return new EventStoreSendMessages(eventStoreConnection);
        }

        IReceiveMessages CreateEventStoreReceiver(string queueName)
        {
            return new EventStoreReceiveMessages(UniqueIdentifier("RebusTestsApplicationId"), queueName, eventStoreConnection);
        }

        public void CleanUp()
        {
        }

        public IReceiveMessages CreateReceiver(string queueName)
        {
            return CreateEventStoreReceiver(queueName);
        }

        string UniqueIdentifier(string prefix)
        {
            return prefix + Guid.NewGuid().ToString("N");
        }
    }
}
