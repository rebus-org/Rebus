using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Auditing.Sagas;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Persistence.InMem;
using Rebus.Sagas;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Auditing;

[TestFixture]
public class TestSagaAuditing : FixtureBase
{
    [Test]
    public void OutputsSagaDataSnapshotsToLog()
    {
        var logger = new ListLoggerFactory(true);

        RunTheTest(logger);

        var lines = logger
            .Where(line => line.Text.Contains("\"CorrelationId\":\"hej\""))
            .ToList();

        Assert.That(lines.Count, Is.EqualTo(3));

        Console.WriteLine($@"

Here we have the logged saga data snapshots:

{string.Join(Environment.NewLine, lines)}

");
    }

    static void RunTheTest(IRebusLoggerFactory logger)
    {
        using var sharedCounter = new SharedCounter(3, "Message counter") { Delay = TimeSpan.FromSeconds(2) };
        using var activator = new BuiltinHandlerActivator();

        activator.Register(() => new SomeSaga(sharedCounter));

        var bus = Configure.With(activator)
            .Logging(l => l.Use(logger))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "saga_snapshots_integration_testerino"))
            .Sagas(s => s.StoreInMemory())
            .Options(o => o.EnableSagaAuditing().OutputToLog())
            .Start();

        Task.WaitAll(
            bus.SendLocal("hej/med dig"),
            bus.SendLocal("hej/igen"),
            bus.SendLocal("hej/igen med dig")
        );

        sharedCounter.WaitForResetEvent(timeoutSeconds: 10);
    }

    class SomeSaga : Saga<SomeSagaData>, IAmInitiatedBy<string>
    {
        readonly SharedCounter _sharedCounter;

        public SomeSaga(SharedCounter sharedCounter)
        {
            _sharedCounter = sharedCounter;
        }

        protected override void CorrelateMessages(ICorrelationConfig<SomeSagaData> config)
        {
            config.Correlate<string>(GetCorrelationId, d => d.CorrelationId);
        }

        public async Task Handle(string message)
        {
            Data.CorrelationId ??= GetCorrelationId(message);

            _sharedCounter.Decrement();
        }

        static string GetCorrelationId(string str) => str.Split('/').First();
    }

    class SomeSagaData : ISagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }
        public string CorrelationId { get; set; }
    }
}