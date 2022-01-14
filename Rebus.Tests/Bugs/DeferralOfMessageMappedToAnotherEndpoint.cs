using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Bugs;

[TestFixture]
public class DeferralOfMessageMappedToAnotherEndpoint : FixtureBase
{
    readonly InMemNetwork _network = new InMemNetwork();

    protected override void SetUp()
    {
        _network.Reset();
    }

    [Test]
    public async Task EndpointMappingsAreUsedWhenDeferringMessages()
    {
        var gotTheString = new ManualResetEvent(false);

        var (a, aStarter) = CreateBus("endpoint-a", c =>
        {
            c.Routing(r => r.TypeBased().Map<string>("endpoint-b"));
        });

        var (b, bStarter) = CreateBus("endpoint-b");

        b.Handle<string>(async str => gotTheString.Set());

        aStarter.Start();
        bStarter.Start();

        await a.Bus.Defer(TimeSpan.FromSeconds(0.1), "HEJ MED DIG MIN VEEEEEEEEEEEEEEEEN");

        gotTheString.WaitOrDie(TimeSpan.FromSeconds(2), "Did not get the expected string within 2 s timeout");
    }

    (BuiltinHandlerActivator, IBusStarter) CreateBus(string queueName, Action<RebusConfigurer> additionalConfiguration = null)
    {
        var activator = new BuiltinHandlerActivator();

        Using(activator);

        var rebusConfigurer = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(_network, queueName))
            .Timeouts(t => t.StoreInMemory());

        additionalConfiguration?.Invoke(rebusConfigurer);

        var starter = rebusConfigurer.Create();

        return (activator, starter);
    }
}