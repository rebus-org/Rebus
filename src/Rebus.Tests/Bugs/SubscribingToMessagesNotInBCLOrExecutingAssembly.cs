using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Persistence.InMemory;
using Rebus.Tests.Integration;
using Rebus.Transports.Msmq;
using JsonMessageSerializer = Rebus.Serialization.Json.JsonMessageSerializer;

namespace Rebus.Tests.Bugs
{
    public class SubscribingToMessagesNotInBCLOrExecutingAssembly : RebusBusMsmqIntegrationTestBase
    {
        [Test]
        public void SubscriptionWorks()
        {
            var inputQueueName = "test.subscriber";
            var messageQueue = new MsmqMessageQueue(inputQueueName).PurgeInputQueue();
            serializer = new JsonMessageSerializer();

            var subscriptionStorage = new InMemorySubscriptionStorage();
            var bus = new RebusBus(new HandlerActivatorForTesting(), messageQueue, messageQueue,
                                   subscriptionStorage, new SagaDataPersisterForTesting(),
                                   this, serializer, new TrivialPipelineInspector());

            bus.Start();
            bus.Subscribe<TheMessage>("test.subscriber");

            Thread.Sleep(500);

            Assert.AreEqual("test.subscriber", subscriptionStorage.GetSubscribers(typeof(TheMessage))[0]);
        }
        
        public class TheMessage
        {
            
        }
    }
}