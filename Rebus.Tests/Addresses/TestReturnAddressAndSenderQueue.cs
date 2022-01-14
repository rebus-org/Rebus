using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Addresses;

[TestFixture]
public class TestReturnAddressAndSenderQueue : FixtureBase
{
    const string QueueName = "queue-headers-tjek";

    BuiltinHandlerActivator _activator;

    IBusStarter _starter;

    protected override void SetUp()
    {
        _activator = new BuiltinHandlerActivator();

        Using(_activator);

        _starter = Configure.With(_activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), QueueName))
            .Create();
    }

    [Test]
    public async Task CorrectlySetsReturnAddressAndSenderAddress_Defaults()
    {
        var messageHandled = new ManualResetEvent(false);
        var returnAddress = "";
        var senderAddress = "";

        _activator.Handle<string>(async (bus, context, message) =>
        {
            var headers = context.Headers;

            returnAddress = headers.GetValueOrNull(Headers.ReturnAddress);
            senderAddress = headers.GetValueOrNull(Headers.SenderAddress);

            messageHandled.Set();
        });

        _starter.Start();

        await _activator.Bus.SendLocal("hej med dig");

        messageHandled.WaitOrDie(TimeSpan.FromSeconds(2));

        Assert.That(returnAddress, Is.EqualTo(QueueName));
        Assert.That(senderAddress, Is.EqualTo(QueueName));
    }

    [Test]
    public async Task CorrectlySetsReturnAddressAndSenderAddress_Overridden()
    {
        var messageHandled = new ManualResetEvent(false);
        var returnAddress = "";
        var senderAddress = "";

        _activator.Handle<string>(async (bus, context, message) =>
        {
            var headers = context.Headers;

            returnAddress = headers.GetValueOrNull(Headers.ReturnAddress);
            senderAddress = headers.GetValueOrNull(Headers.SenderAddress);

            messageHandled.Set();
        });

        _starter.Start();

        await _activator.Bus.SendLocal("hej med dig", new Dictionary<string, string>
        {
            {Headers.SenderAddress, "hooloobooloo-sender-address"},
            {Headers.ReturnAddress, "hooloobooloo-return-address"},
        });

        messageHandled.WaitOrDie(TimeSpan.FromSeconds(2));

        Assert.That(returnAddress, Is.EqualTo("hooloobooloo-return-address"));
        Assert.That(senderAddress, Is.EqualTo(QueueName), "Expected the actual sender's address, because this particular header cannot be set :)");
    }
}