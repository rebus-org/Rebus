using System;
using System.Linq;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Config;

[TestFixture]
public class CheckBusWithCustomName : FixtureBase
{
    readonly ListLoggerFactory _listLoggerFactory = new ListLoggerFactory();

    [Test]
    public void CanCustomizeTheName()
    {
        var activator = new BuiltinHandlerActivator();
            
        Using(activator);

        Configure.With(activator)
            .Logging(l => l.Use(_listLoggerFactory))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "whatever"))
            .Options(o => o.SetBusName("DefaultBus"))
            .Start();

        CleanUpDisposables();

        var lines = _listLoggerFactory.ToList();

        Console.WriteLine(string.Join(Environment.NewLine, lines));

        var startLine = lines.FirstOrDefault(line => line.ToString().Contains(@"Bus ""DefaultBus"" started"))
                        ?? throw new AssertionException(@"Could not find log line containg the text 'Bus ""DefaultBus"" started'");

        Assert.That(startLine.ToString(), Contains.Substring(@"Bus ""DefaultBus"" started"));
    }
}