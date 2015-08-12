using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using System.Linq;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestSubscribeUnsubscribe : RebusBusMsmqIntegrationTestBase
    {
        const string PublisherInputQueueName = "test.sub-unsub.publisher";
        const string Subscriber1InputQueueName = "test.sub-unsub.subscriber1";
        const string Subscriber2InputQueueName = "test.sub-unsub.subscriber2";

        [Test]
        public void CanSubscribeAndUnsubscribeWithAdvancedApi()
        {
            // arrange
            var publisher = CreateBus(PublisherInputQueueName, new HandlerActivatorForTesting()).Start(1);

            var sub1ReceivedEvents = new List<int>();
            var sub2ReceivedEvents = new List<int>();

            var sub1 = CreateBus(Subscriber1InputQueueName,
                                 new HandlerActivatorForTesting()
                                     .Handle<SomeEvent>(e => sub1ReceivedEvents.Add(e.EventNumber))).Start(1);

            var sub2 = CreateBus(Subscriber2InputQueueName,
                                 new HandlerActivatorForTesting()
                                     .Handle<SomeEvent>(e => sub2ReceivedEvents.Add(e.EventNumber))).Start(1);

            // act
            sub1.Advanced.Routing.Subscribe(typeof(SomeEvent));
            sub2.Advanced.Routing.Subscribe(typeof(SomeEvent));

            Thread.Sleep(1.Seconds());

            publisher.Publish(new SomeEvent{EventNumber=1});
            publisher.Batch.Publish(new[] { new SomeEvent { EventNumber = 2 }, new SomeEvent { EventNumber = 3 } });

            Thread.Sleep(1.Seconds());

            sub1.Advanced.Routing.Unsubscribe(typeof(SomeEvent));

            Thread.Sleep(1.Seconds());

            publisher.Publish(new SomeEvent { EventNumber = 4 });
            publisher.Batch.Publish(new[] { new SomeEvent { EventNumber = 5 }, new SomeEvent { EventNumber = 6 } });

            Thread.Sleep(200);

            // assert
            sub1ReceivedEvents.ShouldBe(new[] {1, 2, 3}.ToList());
            sub2ReceivedEvents.ShouldBe(new[] {1, 2, 3, 4, 5, 6}.ToList());
        }

        [Test]
        public void CanSubscribeAndUnsubscribe()
        {
            // arrange
            var publisher = CreateBus(PublisherInputQueueName, new HandlerActivatorForTesting()).Start(1);

            var sub1ReceivedEvents = new List<int>();
            var sub2ReceivedEvents = new List<int>();

            var sub1 = CreateBus(Subscriber1InputQueueName,
                                 new HandlerActivatorForTesting()
                                     .Handle<SomeEvent>(e => sub1ReceivedEvents.Add(e.EventNumber))).Start(1);

            var sub2 = CreateBus(Subscriber2InputQueueName,
                                 new HandlerActivatorForTesting()
                                     .Handle<SomeEvent>(e => sub2ReceivedEvents.Add(e.EventNumber))).Start(1);

            // act
            sub1.Subscribe<SomeEvent>();
            sub2.Subscribe<SomeEvent>();

            Thread.Sleep(1.Seconds());

            publisher.Publish(new SomeEvent{EventNumber=1});
            publisher.Batch.Publish(new[] { new SomeEvent { EventNumber = 2 }, new SomeEvent { EventNumber = 3 } });

            Thread.Sleep(1.Seconds());

            sub1.Unsubscribe<SomeEvent>();

            Thread.Sleep(1.Seconds());

            publisher.Publish(new SomeEvent { EventNumber = 4 });
            publisher.Batch.Publish(new[] { new SomeEvent { EventNumber = 5 }, new SomeEvent { EventNumber = 6 } });

            Thread.Sleep(200);

            // assert
            sub1ReceivedEvents.ShouldBe(new[] {1, 2, 3}.ToList());
            sub2ReceivedEvents.ShouldBe(new[] {1, 2, 3, 4, 5, 6}.ToList());
        }

        public override string GetEndpointFor(Type messageType)
        {
            if (messageType == typeof(SomeEvent))
            {
                return PublisherInputQueueName;
            }

            return base.GetEndpointFor(messageType);
        }

        class SomeEvent
        {
            public int EventNumber { get; set; }
        }
    }
}