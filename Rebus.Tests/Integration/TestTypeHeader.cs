using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Integration;

[TestFixture]
public class TestTypeHeader : FixtureBase
{
    [Test]
    public void SentMessageCarriesTheTypeHeader()
    {
        var activator = Using(new BuiltinHandlerActivator());

        var receivedHeaders = new Dictionary<string, string>();
        var counter = new SharedCounter(1);

        activator.Handle<SomeMessage>(async (bus, context, message) =>
        {
            foreach (var kvp in context.Headers)
            {
                receivedHeaders.Add(kvp.Key, kvp.Value);
            }
            counter.Decrement();
        });

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "type-header-test"))
            .Options(o => o.LogPipeline())
            .Start();

        activator.Bus.SendLocal(new SomeMessage()).Wait();

        counter.WaitForResetEvent();

        Assert.That(receivedHeaders.ContainsKey(Headers.Type), Is.True);
        Assert.That(receivedHeaders[Headers.Type], Is.EqualTo("Rebus.Tests.Integration.SomeMessage, Rebus.Tests"));
    }
}

class SomeMessage { }