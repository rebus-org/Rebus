using System;
using System.Linq;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Pipeline;

[TestFixture]
public class TestPipelineLogging : FixtureBase
{
    ListLoggerFactory _listLoggerFactory;

    protected override void SetUp()
    {
        _listLoggerFactory = new ListLoggerFactory();
    }

    [Test]
    public void CanLogPipelineGood()
    {
        var bus = Configure.With(new BuiltinHandlerActivator())
            .Logging(l => l.Use(_listLoggerFactory))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "test"))
            .Options(o => o.LogPipeline(verbose:true))
            .Start();

        Using(bus);

        var listLoggerFactory = _listLoggerFactory;

        Console.WriteLine(string.Join(Environment.NewLine, listLoggerFactory.Select(l => l.Text)));
        Console.WriteLine();
    }
}