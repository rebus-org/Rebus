using System.Linq;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Routing.TransportMessages;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Routing
{
    public class TestTransportMessageForwarding : FixtureBase
    {
        [Fact]
        public async Task CanForwardToMultipleRecipients()
        {
            var network = new InMemNetwork();
            var activator = new BuiltinHandlerActivator();

            Using(activator);

            var recipients = new[] { "recipient-A", "recipient-B" }.ToList();

            recipients.ForEach(network.CreateQueue);

            Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(network, "forwarder"))
                .Routing(t =>
                {
                    t.AddTransportMessageForwarder(async transportMessage => ForwardAction.ForwardTo(recipients));
                })
                .Start();

            await activator.Bus.SendLocal("HEJ MED DIG!!!");

            var transportMessages = await Task.WhenAll(recipients.Select(async queue =>
            {
                var message = await network.WaitForNextMessageFrom(queue); 
                return message;
            }));

            Assert.Equal(2, transportMessages.Length);
        }
    }
}