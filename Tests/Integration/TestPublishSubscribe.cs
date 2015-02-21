using System.Threading.Tasks;
using NUnit.Framework;
using Rebus2.Activation;
using Rebus2.Bus;
using Rebus2.Config;
using Rebus2.Transport.InMem;

namespace Tests.Integration
{
    [TestFixture]
    public class TestPublishSubscribe : FixtureBase
    {
        const string SubscriberInputQueue = "test.pubsub.input";
        const string PublisherInputQueue = "test.pubsub.input";
        BuiltinHandlerActivator _subscriberHandlers;
        BuiltinHandlerActivator _publisherHandlers;
        IBus _subscriberBus;
        IBus _publisherBus;

        protected override void SetUp()
        {
            var network = new InMemNetwork();

            _subscriberHandlers = new BuiltinHandlerActivator();

            _subscriberBus = Configure.With(_subscriberHandlers)
                .Transport(t => t.UseInMemoryTransport(network, SubscriberInputQueue))
                .Start();

            _publisherHandlers = new BuiltinHandlerActivator();

            _publisherBus = Configure.With(_subscriberHandlers)
                .Transport(t => t.UseInMemoryTransport(network, PublisherInputQueue))
                .Start();
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
    }
}