using System;
using System.Threading;
using NUnit.Framework;
using Rebus.Persistence.InMemory;
using Rebus.Shared;
using Shouldly;

namespace Rebus.Tests.Bugs
{
    public class SubscribingToMessagesNotInBCLOrExecutingAssembly : RebusBusMsmqIntegrationTestBase
    {
        const string PublisherInputQueueName = "test.publisher";
        const string SubscriberInputQueueName = "test.subscriber";
        const string ErrorQueueName = "error";

        protected override void DoSetUp()
        {
            MsmqUtil.Delete(PublisherInputQueueName);
            MsmqUtil.Delete(SubscriberInputQueueName);
            MsmqUtil.Delete(ErrorQueueName);
        }

        [Test]
        public void SubscriptionWorks()
        {
            // arrange
            var subscriptionStorage = new InMemorySubscriptionStorage();
            
            // publisher
            CreateBus(PublisherInputQueueName, new HandlerActivatorForTesting(), subscriptionStorage, new InMemorySagaPersister(), ErrorQueueName).Start(1);
            
            // subscriber
            var subscriber = CreateBus(SubscriberInputQueueName, new HandlerActivatorForTesting()).Start(1);
            
            // act
            subscriber.Subscribe<TheMessage>();

            Thread.Sleep(500);

            // assert
            var subscribers = subscriptionStorage.GetSubscribers(typeof (TheMessage));
            subscribers.Length.ShouldBe(1);
            subscribers[0].ShouldStartWith(SubscriberInputQueueName + "@");
        }

        public override string GetEndpointFor(Type messageType)
        {
            if (messageType == typeof(TheMessage))
            {
                return PublisherInputQueueName;
            }

            return base.GetEndpointFor(messageType);
        }
        
        public class TheMessage
        {
            
        }
    }
}