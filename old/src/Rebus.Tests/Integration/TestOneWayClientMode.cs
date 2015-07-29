using System;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Persistence.InMemory;
using Rebus.Serialization.Json;
using Rebus.Tests.Performance.StressMongo.Factories;
using Shouldly;

namespace Rebus.Tests.Integration
{
    [TestFixture(typeof(MsmqMessageQueueFactory), Category = TestCategories.Msmq)]
    [TestFixture(typeof(RabbitMqMessageQueueFactory), Category = TestCategories.Rabbit)]
    public class TestOneWayClientMode<TFactory> : FixtureBase, IDetermineMessageOwnership where TFactory : IMessageQueueFactory, new()
    {
        TFactory factory;
        const string ReceiverInputQueueName = "test.oneWayClientMode.receiver";

        protected override void DoSetUp()
        {
            factory = new TFactory();
        }

        protected override void DoTearDown()
        {
            factory.CleanUp();
        }

        [Test]
        public void ThrowsWhenSubscribing()
        {
            // arrange
            var bus = Configure.With(CreateAdapter())
                .Transport(t => factory.ConfigureOneWayClientMode(t))
                .MessageOwnership(d => d.Use(this))
                .CreateBus()
                .Start();

            // act
            var exception = Assert.Throws<InvalidOperationException>(bus.Subscribe<string>);

            // assert
            exception.Message.ShouldContain("one-way client mode");
        }

        [Test]
        public void ThrowsWhenDoingSendLocal()
        {
            // arrange
            var bus = Configure.With(CreateAdapter())
                .Transport(t => factory.ConfigureOneWayClientMode(t))
                .MessageOwnership(d => d.Use(this))
                .CreateBus()
                .Start();


            // act
            var exception = Assert.Throws<InvalidOperationException>(() => bus.SendLocal("w00t this should throw!!!"));

            // assert
            exception.Message.ShouldContain("one-way client mode");
        }

        [Test]
        public void CanSendAutomaticallyRoutedMessages()
        {
            var resetEvent = new ManualResetEvent(false);

            CreateBus(ReceiverInputQueueName, new HandlerActivatorForTesting()
                                                  .Handle<string>(str => resetEvent.Set()));

            var bus = Configure.With(CreateAdapter())
                .Transport(t => factory.ConfigureOneWayClientMode(t))
                .MessageOwnership(d => d.Use(this))
                .CreateBus()
                .Start();

            bus.Send("w00t!!!!!!!!!!!1");

            var timeout = 3.Seconds();

            if (!resetEvent.WaitOne(timeout))
            {
                Assert.Fail("Did not receive message within timeout of {0}", timeout);
            }
        }

        [Test]
        public void CanSendExplicitlyRoutedMessages()
        {
            const string receiverQueueName = "test.oneWayClientMode.receiver";
            var resetEvent = new ManualResetEvent(false);

            CreateBus(receiverQueueName, new HandlerActivatorForTesting()
                                             .Handle<string>(str => resetEvent.Set()));

            var bus = Configure.With(CreateAdapter())
                .Transport(t => factory.ConfigureOneWayClientMode(t))
                .CreateBus()
                .Start();

            bus.Advanced.Routing.Send(receiverQueueName, "w00t!!!!!!!!!!!1");

            var timeout = 3.Seconds();

            if (!resetEvent.WaitOne(timeout))
            {
                Assert.Fail("Did not receive message within timeout of {0}", timeout);
            }
        }

        public string GetEndpointFor(Type messageType)
        {
            if (messageType == typeof(string))
            {
                return ReceiverInputQueueName;
            }

            throw new ArgumentException(string.Format("Cannot route {0} - not an expected message type", messageType));
        }

        void CreateBus(string inputQueueName, HandlerActivatorForTesting handlerActivator)
        {
            var queue = factory.GetQueue(inputQueueName);
            var bus = new RebusBus(handlerActivator, queue.Item1, queue.Item2, new InMemorySubscriptionStorage(),
                                   new InMemorySagaPersister(),
                                   this, new JsonMessageSerializer(), new TrivialPipelineInspector(),
                                   new ErrorTracker(inputQueueName + ".error"),
                                   null,
                                   new ConfigureAdditionalBehavior());
            TrackDisposable(bus);
            bus.Start();
        }

        IContainerAdapter CreateAdapter()
        {
            var adapter = new BuiltinContainerAdapter();
            TrackDisposable(adapter);
            return adapter;
        }
    }
}