using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Shutdown;

[TestFixture]
public class DoesNotDoubleDisposeCancellationTokenSource : FixtureBase
{
    readonly ListLoggerFactory _listLoggerFactory = new ListLoggerFactory(outputToConsole: true, detailed: true);

    protected override void SetUp() => _listLoggerFactory.Clear();

    [Test]
    public void WhatTheFixtureSays()
    {
        using (var activator = new BuiltinHandlerActivator())
        {
            Configure.With(activator)
                .Logging(l => l.Use(_listLoggerFactory))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "double-disposal-nono"))
                .Start();


        }
    }

    [Test]
    public void WhatTheFixtureSays_OneWay()
    {
        using (var activator = new BuiltinHandlerActivator())
        {
            Configure.With(activator)
                .Logging(l => l.Use(_listLoggerFactory))
                .Transport(t => t.UseInMemoryTransportAsOneWayClient(new InMemNetwork()))
                .Start();


        }
    }
}