using System.Collections.Concurrent;
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
    IBusStarter _starter;
    BuiltinHandlerActivator _activator;

    protected override void SetUp()
    {
        base.SetUp();

        _network = new();

        _activator = Using(new BuiltinHandlerActivator());

        _starter = Configure.With(_activator)
            .Transport(t => t.UseInMemoryTransport(_network, "some-queue"))
            .Options(o => o.RetryStrategy(maxDeliveryAttempts: 3))
            .Create();
    }

    [Test]
    public async Task ImmediatelyDeadlettersWhenDeliveryCountHeaderExceedsMaxRetries()
    {
        var bus = _starter.Start();

        await bus.SendLocal("HEJ", new Dictionary<string, string> { [Headers.DeliveryCount] = "4" });

        var errorMessage = await _network.WaitForNextMessageFrom("error");

        var messageWasConsumed = _network.GetCount("some-queue");

        Assert.That(errorMessage.Headers[Headers.DeliveryCount], Is.EqualTo("4"));
        Assert.That(messageWasConsumed, Is.Zero, "Queue count was not 0 - looks like the message was not properly consumed");
    }

    [Test]
    [Explicit]
    public async Task DoesNotImmediatelyDeadlettersWhenDeliveryCountHeaderDoesNotExceedMaxRetries()
    {
        var bus = _starter.Start();

        await bus.SendLocal("HEJ", new Dictionary<string, string> { [Headers.DeliveryCount] = "1" });

        var errorMessage = await _network.WaitForNextMessageFrom("error");
        var messageWasConsumed = _network.GetCount("some-queue");

    }
}