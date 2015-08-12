using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestMessageDeferral : FixtureBase
    {
        IBus _bus;
        BuiltinHandlerActivator _handlerActivator;

        protected override void SetUp()
        {
            _handlerActivator = new BuiltinHandlerActivator();

            _bus = Configure.With(_handlerActivator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(true), "test.message.deferral"))
                .Start();

            Using(_bus);
        }

        [Test]
        public async Task CanDeferMessage()
        {
            var messageReceived = new ManualResetEvent(false);
            var deliveryTime = DateTime.MaxValue;

            _handlerActivator.Handle<string>(async s =>
            {
                deliveryTime = DateTime.UtcNow;
                messageReceived.Set();
            });

            var sendTime = DateTime.UtcNow;
            var delay = TimeSpan.FromSeconds(5);

            await _bus.Defer(delay, "hej med dig!");

            messageReceived.WaitOrDie(TimeSpan.FromSeconds(8));

            var timeToBeDelivered = deliveryTime - sendTime;

            Assert.That(timeToBeDelivered, Is.GreaterThanOrEqualTo(delay));
        }
    }
}