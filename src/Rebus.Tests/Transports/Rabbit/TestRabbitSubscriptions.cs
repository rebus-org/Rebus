using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.RabbitMQ;
using Shouldly;
using System.Linq;

namespace Rebus.Tests.Transports.Rabbit
{
    [TestFixture, Category(TestCategories.Rabbit), Description("Verifies that RabbitMQ can be used to implement pub/sub, thus using it to store subscriptions")]
    public class TestRabbitSubscriptions : RabbitMqFixtureBase
    {
        [Test]
        public void SubscriptionsWorkLikeExpectedWhenRabbitManagesThem()
        {
            // arrange
            DeleteQueue("test.rabbitsub.publisher");
            DeleteQueue("test.rabbitsub.sub1");
            DeleteQueue("test.rabbitsub.sub2");
            DeleteQueue("test.rabbitsub.sub3");

            var publisher = PullOneOutOfTheHat("test.rabbitsub.publisher");

            var receivedSub1 = new List<int>();
            var receivedSub2 = new List<int>();
            var receivedSub3 = new List<int>();

            var sub1 = PullOneOutOfTheHat("test.rabbitsub.sub1", receivedSub1.Add);
            var sub2 = PullOneOutOfTheHat("test.rabbitsub.sub2", receivedSub2.Add);
            var sub3 = PullOneOutOfTheHat("test.rabbitsub.sub3", receivedSub3.Add);

            // act
            publisher.Publish(new SomeEvent { Number = 1 });

            Thread.Sleep(200.Milliseconds());

            sub1.Subscribe<SomeEvent>();
            Thread.Sleep(200.Milliseconds());
            publisher.Publish(new SomeEvent { Number = 2 });

            sub2.Subscribe<SomeEvent>();
            Thread.Sleep(200.Milliseconds());
            publisher.Publish(new SomeEvent { Number = 3 });

            sub3.Subscribe<SomeEvent>();
            Thread.Sleep(200.Milliseconds());
            publisher.Publish(new SomeEvent { Number = 4 });

            Thread.Sleep(200.Milliseconds());

            sub3.Unsubscribe<SomeEvent>();
            Thread.Sleep(200.Milliseconds());
            publisher.Publish(new SomeEvent { Number = 5 });

            sub2.Unsubscribe<SomeEvent>();
            Thread.Sleep(200.Milliseconds());
            publisher.Publish(new SomeEvent { Number = 6 });

            sub1.Unsubscribe<SomeEvent>();
            Thread.Sleep(200.Milliseconds());
            publisher.Publish(new SomeEvent { Number = 7 });

            // assert
            receivedSub1.OrderBy(i => i).ToArray().ShouldBe(new[] { 2, 3, 4, 5, 6 });
            receivedSub2.OrderBy(i => i).ToArray().ShouldBe(new[] { 3, 4, 5 });
            receivedSub3.OrderBy(i => i).ToArray().ShouldBe(new[] { 4 });
        }

        [Test]
        public void SubscriptionsWorkLikeExpectedWhenRabbitManagesThemAlsoWhenPublishingWithGenericTypeOtherThanActualType()
        {
            // arrange
            DeleteQueue("test.rabbitsub.publisher");
            DeleteQueue("test.rabbitsub.sub1");

            var receivedSub1 = new List<int>();
            var sub1 = PullOneOutOfTheHat("test.rabbitsub.sub1", receivedSub1.Add);
            var publisher = PullOneOutOfTheHat("test.rabbitsub.publisher");

            // wait a while to allow queues to be initialized
            Thread.Sleep(1.Seconds());

            sub1.Subscribe<SomeEvent>();
            
            Thread.Sleep(0.5.Seconds());
            object someEventAsObject = new SomeEvent { Number = 1 };

            // act
            publisher.Publish(someEventAsObject);

            // assert
            Thread.Sleep(0.5.Seconds());
            receivedSub1.OrderBy(i => i).ToArray().ShouldBe(new[] { 1 });
        }

        class SomeEvent
        {
            public int Number { get; set; }
        }

        IBus PullOneOutOfTheHat(string inputQueueName, Action<int> handler = null)
        {
            var adapter = new BuiltinContainerAdapter();

            if (handler != null) adapter.Handle<SomeEvent>(e => handler(e.Number));

            Configure.With(adapter)
                .Transport(t => t.UseRabbitMq(ConnectionString, inputQueueName, "error").ManageSubscriptions())
                .CreateBus().Start();

            TrackDisposable(adapter);

            return adapter.Bus;
        }
    }
}