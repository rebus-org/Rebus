using System;
using System.Collections.Generic;
using System.Messaging;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Persistence.InMemory;
using Rebus.Serialization.Json;
using Rebus.Transports.Msmq;

namespace Rebus.Tests
{
    /// <summary>
    /// Test base class with helpers for running integration tests with
    /// <see cref="RebusBus"/> and <see cref="MsmqMessageQueue"/>.
    /// </summary>
    public class RebusBusMsmqIntegrationTestBase : IDetermineDestination
    {
        List<RebusBus> buses;

        [SetUp]
        public void SetUp()
        {
            buses = new List<RebusBus>();
        }

        [TearDown]
        public void TearDown()
        {
            buses.ForEach(b => b.Dispose());
        }

        protected RebusBus CreateBus(string inputQueueName, IActivateHandlers activateHandlers)
        {
            var messageQueue = new MsmqMessageQueue(inputQueueName, new JsonMessageSerializer())
                .PurgeInputQueue();
            var bus = new RebusBus(activateHandlers, messageQueue, messageQueue, new InMemorySubscriptionStorage(), this);
            buses.Add(bus);
            return bus;
        }

        protected string PrivateQueueNamed(string queueName)
        {
            return string.Format(@".\private$\{0}", queueName);
        }

        public string GetEndpointFor(Type messageType)
        {
            throw new AssertionException(string.Format("Cannot route {0}", messageType));
        }

        protected static void EnsureQueueExists(string errorQueueName)
        {
            if (!MessageQueue.Exists(errorQueueName))
            {
                MessageQueue.Create(errorQueueName);
            }
        }
    }
}