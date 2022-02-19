using System;
using System.Threading.Tasks;
using Hypothesist;
using Hypothesist.Rebus;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Activation;

[TestFixture]
public class ClientOnly : FixtureBase
{
    private record Message(Guid Id);

    [Test]
    public async Task ActivatorNotNecessary()
    {
        // Arrange
        var message = new Message(Guid.NewGuid());
        var hypothesis = Hypothesis.For<Message>()
            .Any(m => m == message);

        using var activator = new BuiltinHandlerActivator()
            .Register(hypothesis.AsHandler);

        var network = new InMemNetwork();
        using var subscriber = await Subscriber(activator, network);
        using var publisher = Publisher(network);

        // Act
        await publisher.Send(message); // publish doesn't work in this setup, not sure why

        // Assert
        await hypothesis.Validate(TimeSpan.FromSeconds(5));
    }

    private static IBus Publisher(InMemNetwork network) =>
        Configure.OneWayClient()
            .Transport(t => t.UseInMemoryTransportAsOneWayClient(network))
            .Subscriptions(s => s.StoreInMemory()) // req'd also for one way client transports
            .Routing(t => t.TypeBased().Map<Message>("subscriber"))
            .Start();

    private static async Task<IBus> Subscriber(IHandlerActivator activator, InMemNetwork network)
    {
        var subscriber = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(network, "subscriber"))
            .Subscriptions(s => s.StoreInMemory())
            .Routing(t => t.TypeBased().Map<Message>("not-sure")) // req'd also when only subscribing
            .Start();

        await subscriber.Subscribe<Message>();
        return subscriber;
    }
}