using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Routing.TopicBased;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestPublishSubscribe : FixtureBase
    {
        static readonly string SubscriberInputQueue = TestConfig.QueueName("test.pubsub.subscriber@" + Environment.MachineName);
        static readonly string PublisherInputQueue = TestConfig.QueueName("test.pubsub.publisher@" + Environment.MachineName);

        BuiltinHandlerActivator _subscriberHandlers;

        IBus _subscriberBus;
        IBus _publisherBus;

        protected override void SetUp()
        {
            AdjustLogging(LogLevel.Info);

            var network = new InMemNetwork(true);

            _subscriberHandlers = new BuiltinHandlerActivator();

            _subscriberBus = Configure.With(_subscriberHandlers)
                .Transport(t => t.UseInMemoryTransport(network, SubscriberInputQueue))
                //.Transport(t => t.UseMsmq(SubscriberInputQueue))
                .Routing(r => r.TopicBased().Map("someTopic", PublisherInputQueue))
                .Start();

            Using(_subscriberBus);

            _publisherBus = Configure.With(new BuiltinHandlerActivator())
                .Transport(t => t.UseInMemoryTransport(network, PublisherInputQueue))
                //.Transport(t => t.UseMsmq(PublisherInputQueue))
                .Routing(r => r.TopicBased().Map("someTopic", PublisherInputQueue))
                .Start();

            Using(_publisherBus);
        }

        [Test]
        public async Task SusbcriberDoesNotReceiveMessagesWhenItHasNotYetSubscribed()
        {
            var receivedStringMessage = false;

            _subscriberHandlers.Handle<string>(async str =>
            {
                receivedStringMessage = true;
            });

            await _publisherBus.Publish("someTopic", "hej med dig min ven!!!!!");

            await Task.Delay(2000);

            Assert.That(receivedStringMessage, Is.False, "Did not expect to receive the string message in the subscriber because it has not subscribed");
        }

        [Test]
        public async Task SubcriberReceivesMessagesWhenItHasSubscribed()
        {
            var subscriberReceivedMessage = new ManualResetEvent(false);
            const string topicName = "someTopic";

            await _subscriberBus.Subscribe(topicName);

            _subscriberHandlers.Handle<string>(async str =>
            {
                subscriberReceivedMessage.Set();
            });

            await Task.Delay(1000);

            await _publisherBus.Publish(topicName, "hej med dig min ven!!!!!");

            subscriberReceivedMessage.WaitOrDie(TimeSpan.FromSeconds(30), "Expected to have received a message");
        }

        [Test]
        public async Task SubcriberDoesNotReceiveMessagesWhenItHasSubscribedAndUnsubscribed()
        {
            var receivedStringMessage = false;
            const string topicName = "someTopic";

            _subscriberHandlers.Handle<string>(async str =>
            {
                receivedStringMessage = true;
            });

            await _subscriberBus.Subscribe(topicName);
            
            await Task.Delay(1000);

            await _subscriberBus.Unsubscribe(topicName);

            await Task.Delay(1000);

            await _publisherBus.Publish(topicName, "hej med dig min ven!!!!!");

            await Task.Delay(1000);

            Assert.That(receivedStringMessage, Is.False, "Did not expect to receive the string message in the subscriber because it has been unsubscribed");
        }
    }
}