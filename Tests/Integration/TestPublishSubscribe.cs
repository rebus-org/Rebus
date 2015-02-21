using System.Threading.Tasks;
using NUnit.Framework;
using Rebus2.Activation;
using Rebus2.Bus;
using Rebus2.Config;
using Rebus2.Logging;
using Rebus2.Routing.TypeBased;
using Rebus2.Transport.InMem;

namespace Tests.Integration
{
    [TestFixture]
    public class TestPublishSubscribe : FixtureBase
    {
        const string SubscriberInputQueue = "test.pubsub.subscriber";
        const string PublisherInputQueue = "test.pubsub.publisher";

        BuiltinHandlerActivator _subscriberHandlers;

        IBus _subscriberBus;
        IBus _publisherBus;

        protected override void SetUp()
        {
            AdjustLogging(LogLevel.Warn);

            var network = new InMemNetwork(true);

            _subscriberHandlers = new BuiltinHandlerActivator();

            _subscriberBus = Configure.With(_subscriberHandlers)
                .Transport(t => t.UseInMemoryTransport(network, SubscriberInputQueue))
                .Routing(r => r.SimpleTypeBased().Map("someTopic", PublisherInputQueue))
                .Start();

            TrackDisposable(_subscriberBus);

            _publisherBus = Configure.With(new BuiltinHandlerActivator())
                .Transport(t => t.UseInMemoryTransport(network, PublisherInputQueue))
                .Start();

            TrackDisposable(_publisherBus);
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
        public async Task SusbcriberReceivesMessagesWhenItHasSubscribed()
        {
            const string topicName = "someTopic";

            var receivedStringMessage = false;

            await _subscriberBus.Subscribe(topicName);

            _subscriberHandlers.Handle<string>(async str =>
            {
                receivedStringMessage = true;
            });

            await _publisherBus.Publish(topicName, "hej med dig min ven!!!!!");

            await Task.Delay(20000);

            Assert.That(receivedStringMessage, Is.True, "Expected to have received a string");
        }
    }
}