using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Assumptions;

[TestFixture]
public class TestConfigureAwaitInRebusHandler : FixtureBase
{
    [Test]
    public async Task WhatHappensQuestionMark()
    {
        var gotMessage2 = new ManualResetEvent(false);

        var activator = new BuiltinHandlerActivator();

        Using(activator);

        activator.Handle<Message1>(async (bus, m1) =>
        {
            await bus.Send(new Message2()).ConfigureAwait(false);

            throw new Exception("-|The Most Mundane Exception In The World|-");
        });

        activator.Handle<Message2>(async m2 => gotMessage2.Set());

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "daw do"))
            .Routing(r => r.TypeBased().Map<Message1>("daw do").Map<Message2>("daw do"))
            .Start();

        await activator.Bus.Send(new Message1());

        Assert.Throws<AssertionException>(() => gotMessage2.WaitOrDie(TimeSpan.FromSeconds(2)));
    }

    class Message1 { }
    class Message2 { }
}