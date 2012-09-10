using System;
using System.Collections.Generic;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Persistence.InMemory;
using Rebus.RabbitMQ;
using Rebus.Serialization.Json;
using Rebus.Tests.Transports.Rabbit;
using Rebus.Transports.Msmq;

namespace Rebus.Tests.Integration.Factories
{
    public interface IBusFactory
    {
        IAdvancedBus CreateBus(string inputQueueName, IActivateHandlers handlerActivator);
        void Cleanup();
        void StartAll();
    }

    abstract class BusFactoryBase : IBusFactory
    {
        readonly List<IStartableBus> startables = new List<IStartableBus>();
        readonly List<IDisposable> disposables = new List<IDisposable>();

        public IAdvancedBus CreateBus(string inputQueueName, IActivateHandlers handlerActivator)
        {
            var transport = CreateTransport(inputQueueName);
            
            var bus = new RebusBus(handlerActivator, transport, transport, new InMemorySubscriptionStorage(),
                                   new InMemorySagaPersister(), new ThrowingEndpointMapper(),
                                   new JsonMessageSerializer(),
                                   new TrivialPipelineInspector(), new ErrorTracker("error"));
            startables.Add(bus);

            return bus;
        }

        protected abstract IDuplexTransport CreateTransport(string inputQueueName);

        public void Cleanup()
        {
            disposables.ForEach(d => d.Dispose());
            disposables.Clear();
        }

        public void StartAll()
        {
            startables.ForEach(s => s.Start());
            startables.Clear();
        }
    }

    class RabbitBusFactory : BusFactoryBase
    {
        protected override IDuplexTransport CreateTransport(string inputQueueName)
        {
            return new RabbitMqMessageQueue(RabbitMqFixtureBase.ConnectionString, inputQueueName);
        }
    }

    class MsmqBusFactory : BusFactoryBase
    {
        protected override IDuplexTransport CreateTransport(string inputQueueName)
        {
            return new MsmqMessageQueue(inputQueueName);
        }
    }
}