using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Retry.ErrorTracking;

[TestFixture]
public class TestNativeDeliveryCounter : FixtureBase
{
    InMemNetwork _network;
    IBus _bus;

    protected override void SetUp()
    {
        base.SetUp();

        _network = new();

        var activator = Using(new BuiltinHandlerActivator());

        _bus = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(_network, "some-queue"))
            .Options(o => o.RetryStrategy(maxDeliveryAttempts: 3))
            .Start();
    }

    [Test]
    public async Task ImmediatelyDeadlettersWhenDeliveryCountHeaderExceedsMaxRetries()
    {
        await _bus.SendLocal("HEJ", new Dictionary<string, string> { [Headers.DeliveryCount] = "4" });

        var errorMessage = await _network.WaitForNextMessageFrom("error");

        var messageWasConsumed = _network.GetCount("some-queue");

        Assert.That(errorMessage.Headers[Headers.DeliveryCount], Is.EqualTo("4"));
        Assert.That(messageWasConsumed, Is.Zero, "Queue count was not 0 - looks like the message was not properly consumed");
    }
}