using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Newtonsoft.JsonNET;
using Rebus.Persistence.InMemory;
using Rebus.Transports.Rabbit;
using log4net.Config;

namespace Rebus.Tests.Transports.Rabbit
{
    public abstract class RabbitMqFixtureBase : IDetermineDestination
    {
        protected const string ConnectionString = "amqp://guest:guest@localhost";

        List<RebusBus> buses;

        static RabbitMqFixtureBase()
        {
            XmlConfigurator.Configure();
        }

        [SetUp]
        public void SetUp()
        {
            buses = new List<RebusBus>();
            DoSetUp();
        }

        protected virtual void DoSetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
            buses.ForEach(b => b.Dispose());
            DoTearDown();
        }

        protected virtual void DoTearDown()
        {
        }

        protected RebusBus CreateBus(string inputQueueName, IActivateHandlers handlerActivator)
        {
            var rabbitMqMessageQueue = new RabbitMqMessageQueue(ConnectionString, inputQueueName).PurgeInputQueue();

            var bus = new RebusBus(handlerActivator, rabbitMqMessageQueue, rabbitMqMessageQueue,
                                   new InMemorySubscriptionStorage(), new InMemorySagaPersister(), this,
                                   new JsonMessageSerializer(), new TrivialPipelineInspector());

            buses.Add(bus);

            return bus;
        }

        public virtual string GetEndpointFor(Type messageType)
        {
            throw new NotImplementedException(string.Format("Don't know the destination of {0} - override this method in derived classes", messageType));
        }
    }
}