using System;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Compression;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Integration;

[TestFixture]
public class TestStartingAgainAndAgain : FixtureBase
{
    [Test]
    public void JustDoIt()
    {
        var activator = Using(new BuiltinHandlerActivator());
        var rebusConfigurer = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "bimse"))
            .Options(o =>
            {
                o.SetMaxParallelism(1);
                o.SetNumberOfWorkers(1);
                o.EnableCompression();
            });

        rebusConfigurer.Start();

        Assert.Throws<InvalidOperationException>(() =>
        {
            rebusConfigurer.Start();
        });
    }
}