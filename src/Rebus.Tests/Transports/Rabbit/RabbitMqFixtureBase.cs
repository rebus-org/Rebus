using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Persistence.InMemory;
using Rebus.RabbitMQ;
using Rebus.Serialization.Json;
using log4net.Config;

namespace Rebus.Tests.Transports.Rabbit
{
    public abstract class RabbitMqFixtureBase : IDetermineDestination
    {
        public const string ConnectionString = "amqp://guest:guest@localhost";

        protected List<IDisposable> toDispose;

        static RabbitMqFixtureBase()
        {
            XmlConfigurator.Configure();
        }

        [SetUp]
        public void SetUp()
        {
            toDispose = new List<IDisposable>();
            DoSetUp();
        }

        protected virtual void DoSetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
            toDispose.ForEach(b => b.Dispose());
            DoTearDown();
            RebusLoggerFactory.Reset();
        }

        protected virtual void DoTearDown()
        {
        }

        protected RebusBus CreateBus(string inputQueueName, IActivateHandlers handlerActivator)
        {
            var rabbitMqMessageQueue = new RabbitMqMessageQueue(ConnectionString, inputQueueName, inputQueueName + ".error").PurgeInputQueue();

            var bus = new RebusBus(handlerActivator, rabbitMqMessageQueue, rabbitMqMessageQueue,
                                   new InMemorySubscriptionStorage(), new InMemorySagaPersister(), this,
                                   new JsonMessageSerializer(), new TrivialPipelineInspector(),
                                   new ErrorTracker(inputQueueName + ".error"));

            toDispose.Add(bus);
            toDispose.Add(rabbitMqMessageQueue);

            return bus;
        }

        public virtual string GetEndpointFor(Type messageType)
        {
            throw new NotImplementedException(string.Format("Don't know the destination of {0} - override this method in derived classes", messageType));
        }
    }
}