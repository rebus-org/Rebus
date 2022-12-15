using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Injection;
using Rebus.Persistence.InMem;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Activation;

[TestFixture]
public class ClientOnly : FixtureBase
{
    record Message(Guid Id);
    
    [Test]
    [Description("Verifies that a Rebus instance configured with Configure.OneWayClient().(...) cannot be configured with an input queue")]
    public void TestOneWayClientValidation()
    {
        var resolutionException = Assert.Throws<ResolutionException>(() =>
        {
            _ = Configure.OneWayClient()
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "oh no 💀"))
                .Start();
        });

        Assert.That(resolutionException!.InnerException, Is.TypeOf<InvalidOperationException>());

        var invalidOperationException = (InvalidOperationException)resolutionException.InnerException;

        Console.WriteLine(invalidOperationException);
    }

    static IBus Publisher(InMemNetwork network, InMemorySubscriberStore subscriberStore) =>
        Configure.OneWayClient()
            .Transport(t => t.UseInMemoryTransportAsOneWayClient(network))
            .Subscriptions(s => s.StoreInMemory(subscriberStore)) // req'd also for one way client transports
            .Start();

    static async Task<IBus> Subscriber(IHandlerActivator activator, InMemNetwork network, InMemorySubscriberStore subscriberStore)
    {
        var subscriber = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(network, "subscriber"))
            .Subscriptions(s => s.StoreInMemory(subscriberStore))
            .Start();

        await subscriber.Subscribe<Message>();
        return subscriber;
    }
}