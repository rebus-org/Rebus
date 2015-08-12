using System;
using System.Collections.Generic;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Persistence.InMemory;
using Rebus.Serialization.Json;
using Rebus.Timeout;

namespace Rebus.Tests.Integration.Factories
{
    abstract class BusFactoryBase : IBusFactory
    {
        protected const string ErrorQueueName = "error";
        readonly List<RebusBus> startables = new List<RebusBus>();
        readonly List<IDisposable> disposables = new List<IDisposable>();

        public IBus CreateBus(string inputQueueName, IActivateHandlers handlerActivator, IStoreTimeouts storeTimeouts)
        {
            var transport = CreateTransport(inputQueueName);

            var bus = new RebusBus(handlerActivator, transport, transport,
                                   new InMemorySubscriptionStorage(),
                                   new InMemorySagaPersister(),
                                   new ThrowingEndpointMapper(),
                                   new JsonMessageSerializer(),
                                   new TrivialPipelineInspector(),
                                   new ErrorTracker(ErrorQueueName),
                                   storeTimeouts, new ConfigureAdditionalBehavior());
            startables.Add(bus);
            disposables.Add(bus);

            return bus;
        }

        public IBus CreateBus(string inputQueueName, IActivateHandlers handlerActivator)
        {
            return CreateBus(inputQueueName, handlerActivator, null);
        }

        protected abstract IDuplexTransport CreateTransport(string inputQueueName);

        public virtual void Cleanup()
        {
            disposables.ForEach(d => d.Dispose());
            disposables.Clear();
        }

        protected void RegisterForDisposal(IDisposable disposable)
        {
            disposables.Add(disposable);
        }

        public virtual void StartAll()
        {
            startables.ForEach(s => s.Start(1));
            startables.Clear();
        }
    }
}