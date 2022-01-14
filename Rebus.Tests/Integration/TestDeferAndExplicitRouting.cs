using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
// ReSharper disable ArgumentsStyleAnonymousFunction
// ReSharper disable ArgumentsStyleNamedExpression
#pragma warning disable 1998

namespace Rebus.Tests.Integration;

[TestFixture]
public class TestDeferAndExplicitRouting : FixtureBase
{
    readonly InMemNetwork _inMemNetwork = new InMemNetwork();

    protected override void SetUp() => _inMemNetwork.Reset();

    [Test]
    public async Task GreatName()
    {
        var messageWasReceived = new ManualResetEvent(false);

        // create three receivers
        CreateBus("receiver1");
        CreateBus("receiver2");
        CreateBus("receiver3", activator => activator.Handle<string>(async s => messageWasReceived.Set()));

        var sender = CreateBus("sender", configureRouting: router =>
        {
            // configure ordinary endpoint mappings for the two first receivers
            router
                .Map<string>("receiver1")
                .MapFallback("receiver2");
        });

        // defer and explicitly route the message to receiver3
        await sender.Advanced.Routing.Defer("receiver3", TimeSpan.FromSeconds(1), "HEJ MED DIG MIN VEN!!!!!");

        messageWasReceived.WaitOrDie(TimeSpan.FromSeconds(3));
    }

    IBus CreateBus(string inputQueueName, Action<BuiltinHandlerActivator> configureActivator = null, Action<TypeBasedRouterConfigurationExtensions.TypeBasedRouterConfigurationBuilder> configureRouting = null)
    {
        var activator = new BuiltinHandlerActivator();

        Using(activator);

        configureActivator?.Invoke(activator);

        return Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(_inMemNetwork, inputQueueName))
            .Timeouts(t => t.StoreInMemory())
            .Routing(r => configureRouting?.Invoke(r.TypeBased()))
            .Start();
    }
}