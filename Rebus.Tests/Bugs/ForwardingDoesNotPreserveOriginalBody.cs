using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Encryption;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
// ReSharper disable ArgumentsStyleLiteral
#pragma warning disable 1998

namespace Rebus.Tests.Bugs;

[TestFixture]
public class ForwardingDoesNotPreserveOriginalBody : FixtureBase
{
    BuiltinHandlerActivator _client;
    IBusStarter _clientStarter;
    BuiltinHandlerActivator _forwarder;
    IBusStarter _forwarderStarter;
    BuiltinHandlerActivator _receiver;
    IBusStarter _receiverStarter;

    protected override void SetUp()
    {
        var network = new InMemNetwork(outputEventsToConsole: true);

        (_client, _clientStarter) = CreateBus(network);
        (_forwarder, _forwarderStarter) = CreateBus(network, "forwarder");
        (_receiver, _receiverStarter) = CreateBus(network, "receiver");
    }

    void Start()
    {
        _clientStarter.Start();
        _forwarderStarter.Start();
        _receiverStarter.Start();
    }

    [Test]
    public async Task ReceivedMessageIsTheOriginal()
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

        Start();

        await _client.Bus.Advanced.Routing.Send("forwarder", "hej du");

        gotTheExpectedStringMessage.WaitOrDie(TimeSpan.FromSeconds(2));
    }

    (BuiltinHandlerActivator, IBusStarter) CreateBus(InMemNetwork network, string queueName = null)
    {
        var activator = new BuiltinHandlerActivator();

        Using(activator);

        var starter = Configure.With(activator)
            .Logging(l => l.Console(LogLevel.Warn))
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
            .Options(o => o.EnableEncryption("d2f6QgE0ITuV++fM+BjzVJ1O+LClb3QdUsraWl2qlB4="))
            .Create();

        return (activator, starter);
    }
}