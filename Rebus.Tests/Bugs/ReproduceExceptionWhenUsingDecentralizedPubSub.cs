using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Topic;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Bugs;

[TestFixture]
public class ReproduceExceptionWhenUsingDecentralizedPubSub : FixtureBase
{
    [Test]
    [Explicit("Added this test case because of https://github.com/rebus-org/Rebus/issues/1051")]
    public async Task ReproduceIt()
    {
        using var activator = new BuiltinHandlerActivator();

        var bus = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "my-address"))
            .Routing(r => r.TypeBased().Map<MyMessage>("some-address"))
            .Options(o => o.Decorate<ITopicNameConvention>(_ => new MyConvention()))
            .Start();

        await bus.Subscribe<MyMessage>(); // Throws an exception
    }

    class MyConvention : ITopicNameConvention
    {
        public string GetTopic(Type eventType) => eventType.Name.ToLowerInvariant();
    }

    class MyMessage { }
}