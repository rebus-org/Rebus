using System;
using System.Linq;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;

#pragma warning disable 1998

namespace Rebus.Tests.Pipeline;

[TestFixture]
public class TestOrdinaryLogLevels : FixtureBase
{
    BuiltinHandlerActivator _activator;
    ListLoggerFactory _logs;

    protected override void SetUp()
    {
        _activator = Using(new BuiltinHandlerActivator());
        _logs = new ListLoggerFactory(outputToConsole: true, detailed: true);

        Configure.With(_activator)
            .Logging(l => l.Use(_logs))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "log-levels"))
            .Options(o =>
            {
                o.SetNumberOfWorkers(1);
                o.SetMaxParallelism(1);
            })
            .Start();
    }

    [Test]
    public void DoesNotLogWarningsUnderNormalUse()
    {
        var counter = new SharedCounter(3);
        _activator.AddHandlerWithBusTemporarilyStopped<string>(async str => counter.Decrement());

        _activator.Bus.SendLocal("hej").Wait();
        _activator.Bus.SendLocal("med").Wait();
        _activator.Bus.SendLocal("dig").Wait();

        counter.WaitForResetEvent();

        CleanUpDisposables();

        var logLinesWarnLevelOrAbove = _logs.Where(l => l.Level >= LogLevel.Warn).ToList();

        Assert.That(logLinesWarnLevelOrAbove.Any(), Is.False, $@"Got the following log lines >= WARN:

{string.Join(Environment.NewLine, logLinesWarnLevelOrAbove)}");
    }
}