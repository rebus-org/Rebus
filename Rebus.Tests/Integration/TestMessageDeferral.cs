using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Integration
{
    public class TestMessageDeferral : FixtureBase
    {
        private readonly IBus _bus;
        private readonly BuiltinHandlerActivator _handlerActivator;

        public TestMessageDeferral()
        {
            _handlerActivator = new BuiltinHandlerActivator();

            _bus = Configure.With(_handlerActivator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(true), "test.message.deferral"))
                .Start();

            Using(_bus);
        }

        [Fact]
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

            Assert.True(timeToBeDelivered >= delay);
        }
    }
}