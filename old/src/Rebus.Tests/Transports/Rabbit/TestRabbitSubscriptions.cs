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
        static readonly string[] Queues = { "test.rabbitsub.sub1", "test.rabbitsub.sub2", "test.rabbitsub.sub3", "test.rabbitsub.publisher" };

        protected override void DoSetUp()
        {
            RemoveQueuesAndExchanges();
        }

        protected override void DoTearDown()
        {
            RemoveQueuesAndExchanges();
        }

        static void RemoveQueuesAndExchanges()
        {
            DeleteExchange("Rebus");
            foreach (var queue in Queues)
            {
                DeleteQueue(queue);
                DeleteQueue("ex-" + queue);
                DeleteExchange("ex-" + queue);
            }

            DeleteQueue("test.rabbitsub.publisher");
            DeleteQueue("test.rabbitsub.sub1");
            DeleteQueue("test.rabbitsub.sub2");
            DeleteQueue("test.rabbitsub.sub3");
        }

        [Test]
        public void SubscriptionsWorkLikeExpectedWhenRabbitManagesThem()
        {
            // arrange
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

        [TestCase(false)]
        [TestCase(true)]
        public void SubscriptionsWorkLikeExpectedWhenRabbitManagesThemUsingOneExchangePerMessageType(bool usingExchangeAsInput)
        {
            // arrange
            var publisher = PullOneOutOfTheHat(Queues[3], oneExchangePerType: true, inputExchange: usingExchangeAsInput ? "ex-" + Queues[3] : null);

            var receivedSub1 = new List<int>();
            var receivedSub2 = new List<int>();
            var receivedSub3 = new List<int>();

            var sub1 = PullOneOutOfTheHat(Queues[0], receivedSub1.Add, oneExchangePerType: true, inputExchange: usingExchangeAsInput ? "ex-" + Queues[0] : null);
            var sub2 = PullOneOutOfTheHat(Queues[1], receivedSub2.Add, oneExchangePerType: true, inputExchange: usingExchangeAsInput ? "ex-" + Queues[1] : null);
            var sub3 = PullOneOutOfTheHat(Queues[2], receivedSub3.Add, oneExchangePerType: true, inputExchange: usingExchangeAsInput ? "ex-" + Queues[2] : null);

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
            DeclareExchange("Rebus", "topic", passive: true).ShouldBe(false);
            DeclareExchange(typeof(SomeEvent).FullName, "topic", passive: true).ShouldBe(true);
            foreach (var q in Queues)
            {
                DeclareExchange("ex-" + q, "fanout", passive: true).ShouldBe(usingExchangeAsInput);
			    // Ensure no spurious queue names are created.
			    DeclareQueue("ex-" + q, passive: true).ShouldBe(false);
            }

        }

        class SomeEvent
        {
            public int Number { get; set; }
        }

        IBus PullOneOutOfTheHat(string inputQueueName, Action<int> handler = null, bool oneExchangePerType = false, string inputExchange = null)
        {
            var adapter = new BuiltinContainerAdapter();

            if (handler != null) adapter.Handle<SomeEvent>(e => handler(e.Number));

            Configure.With(adapter)
                .Transport(t => {
                    var obj = t.UseRabbitMq(ConnectionString, inputQueueName, "error").ManageSubscriptions();
                    if (oneExchangePerType) obj.UseOneExchangePerMessageTypeRouting();
                    if (inputExchange != null) obj.UseExchangeAsInputAddress(inputExchange);
                })
                .CreateBus().Start();

            TrackDisposable(adapter);

            return adapter.Bus;
        }
    }
}