using System;
using System.Linq;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Pipeline
{
    public class TestOrdinaryLogLevels : FixtureBase
    {
        readonly BuiltinHandlerActivator _activator;
        readonly ListLoggerFactory _logs;

        public TestOrdinaryLogLevels()
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

        [Fact]
        public void DoesNotLogWarningsUnderNormalUse()
        {
            var counter = new SharedCounter(3);
            _activator.Handle<string>(async str => counter.Decrement());

            _activator.Bus.SendLocal("hej").Wait();
            _activator.Bus.SendLocal("med").Wait();
            _activator.Bus.SendLocal("dig").Wait();

            counter.WaitForResetEvent();

            CleanUpDisposables();

            var logLinesWarnLevelOrAbove = _logs.Where(l => l.Level >= LogLevel.Warn).ToList();

            Assert.False(logLinesWarnLevelOrAbove.Any(), $@"Got the following log lines >= WARN:
                {string.Join(Environment.NewLine, logLinesWarnLevelOrAbove)}");
        }
    }
}