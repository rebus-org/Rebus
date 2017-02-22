using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Encryption;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
// ReSharper disable ArgumentsStyleLiteral
#pragma warning disable 1998

namespace Rebus.Tests.Bugs
{
    [TestFixture]
    public class ForwardingDoesNotPreserveOriginalBody : FixtureBase
    {
        BuiltinHandlerActivator _client;
        BuiltinHandlerActivator _forwarder;
        BuiltinHandlerActivator _receiver;
        BuiltinHandlerActivator _errorHandler;

        protected override void SetUp()
        {
            var network = new InMemNetwork(outputEventsToConsole: true);

            _client = CreateBus(network);
            _errorHandler = CreateBus(network, "error");
            _forwarder = CreateBus(network, "forwarder");
            _receiver = CreateBus(network, "receiver");
        }

        [Test]
        public async Task ReceivedMessageIsTheOriginal_PoisonMessage()
        {
            var gotTheExpectedStringMessage = new ManualResetEvent(false);

            _forwarder.Handle<string>(async (bus, _) =>
            {
                // "forward" the transport message to the error handler
                throw new AccessViolationException("WHOA?!?!");
            });

            _errorHandler.Handle<string>(async (bus, _) =>
            {
                // retry message
                await bus.Advanced.TransportMessage.Forward("receiver");
            });

            _receiver.Handle<string>(async stringMessage =>
            {
                if (stringMessage == "hej du")
                {
                    gotTheExpectedStringMessage.Set();
                }
            });

            await _client.Bus.Advanced.Routing.Send("forwarder", "hej du");

            gotTheExpectedStringMessage.WaitOrDie(TimeSpan.FromSeconds(2));
        }

        [Test]
        public async Task ReceivedMessageIsTheOriginal_ForwardingUsingTransportMessageApi()
        {
            var gotTheExpectedStringMessage = new ManualResetEvent(false);

            _forwarder.Handle<string>(async (bus, _) =>
            {
                // forward the transport message to the receiver
                await bus.Advanced.TransportMessage.Forward("receiver");
            });

            _receiver.Handle<string>(async stringMessage =>
            {
                if (stringMessage == "hej du")
                {
                    gotTheExpectedStringMessage.Set();
                }
            });

            await _client.Bus.Advanced.Routing.Send("forwarder", "hej du");

            gotTheExpectedStringMessage.WaitOrDie(TimeSpan.FromSeconds(2));
        }

        BuiltinHandlerActivator CreateBus(InMemNetwork network, string queueName = null)
        {
            var activator = new BuiltinHandlerActivator();

            Using(activator);

            Configure.With(activator)
                .Logging(l => l.None())
                .Transport(t =>
                {
                    if (queueName == null)
                    {
                        t.UseInMemoryTransportAsOneWayClient(network);
                    }
                    else
                    {
                        t.UseInMemoryTransport(network, queueName);
                    }
                })
                .Options(o =>
                {
                    o.EnableEncryption("d2f6QgE0ITuV++fM+BjzVJ1O+LClb3QdUsraWl2qlB4=");
                    o.SimpleRetryStrategy(maxDeliveryAttempts: 1);
                })
                .Start();

            return activator;
        }
    }
}