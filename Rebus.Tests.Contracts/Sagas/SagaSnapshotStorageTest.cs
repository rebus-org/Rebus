using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Auditing.Sagas;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Logging;
using Rebus.Persistence.InMem;
using Rebus.Sagas;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Contracts.Sagas;

public abstract class SagaSnapshotStorageTest<TFactory> : FixtureBase where TFactory : ISagaSnapshotStorageFactory, new()
{
    BuiltinHandlerActivator _activator;
    IBus _bus;
    TFactory _factory;
    ListLoggerFactory _logger;
    IBusStarter _starter;

    protected override void SetUp()
    {
        _factory = new TFactory();

        _activator = new BuiltinHandlerActivator();

        Using(_activator);

        _logger = new ListLoggerFactory(true);

        _starter = Configure.With(_activator)
            .Logging(l => l.Use(_logger))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "saga_snapshots_integration_testerino"))
            .Sagas(s => s.StoreInMemory())
            .Options(o => o.EnableSagaAuditing().Register(c => _factory.Create()))
            .Create();

        _bus = _starter.Bus;
    }

    [Test]
    public async Task DoesNotFailWhenSagaDataCouldNotBeFound()
    {
        var sharedCounter = Using(new SharedCounter(1));

        _activator.Register(() => new SomeSaga(sharedCounter, false));
        _starter.Start();

        await _bus.SendLocal(new MessageThatCanNeverBeCorrelated());

        await Task.Delay(2000);

        var lines = _logger.ToList();

        var warningsOrAbove = lines.Where(l => l.Level >= LogLevel.Warn).ToList();

        Assert.That(warningsOrAbove.Any(), Is.False,
            $@"Didn't expect warnings or errors, but got this:

{string.Join(Environment.NewLine, warningsOrAbove)}

as part of this:

{string.Join(Environment.NewLine, lines)}");
    }

    [Test]
    public void CreatesSnapshotOfSagaData()
    {
        var sharedCounter = new SharedCounter(3, "Message counter")
        {
            Delay = TimeSpan.FromSeconds(2)
        };

        Using(sharedCounter);

        _activator.Register(() => new SomeSaga(sharedCounter, false));
        _starter.Start();

        Task.WaitAll(
            _bus.SendLocal("hej/med dig"),
            _bus.SendLocal("hej/igen"),
            _bus.SendLocal("hej/igen med dig")
        );

        sharedCounter.WaitForResetEvent(timeoutSeconds: 10);

        var allSnapshots = _factory.GetAllSnapshots().ToList();

        Assert.That(allSnapshots.Count, Is.EqualTo(3));

        Assert.That(allSnapshots.All(s => s.SagaData.Id == allSnapshots.First().SagaData.Id),
            "Not all snapshots had the same saga ID!");

        Assert.That(allSnapshots.OrderBy(s => s.SagaData.Revision).Select(s => s.SagaData.Revision), Is.EqualTo(new[] { 0, 1, 2 }),
            "Expected the three initial revisions");
    }

    [Test]
    public void CreatesSnapshotOfSagaDataAlsoWhenImmediatelyMarkingAsComplete()
    {
        var sharedCounter = new SharedCounter(3, "Message counter")
        {
            Delay = TimeSpan.FromSeconds(1)
        };

        Using(sharedCounter);

        _activator.Register(() => new SomeSaga(sharedCounter, true));
        _starter.Start();

        Task.WaitAll(
            _bus.SendLocal("hej/med dig"),
            _bus.SendLocal("hej/igen"),
            _bus.SendLocal("hej/igen med dig")
        );

        sharedCounter.WaitForResetEvent();

        Thread.Sleep(500); //< be sure that the snapshot has made it to the database

        var allSnapshots = _factory.GetAllSnapshots().ToList();

        Assert.That(allSnapshots.Count, Is.EqualTo(3));

        Assert.That(allSnapshots.All(s => s.SagaData.Revision == 0), "Not all revisions were initial!: {0}", allSnapshots.Select(s => s.SagaData.Revision));

        Assert.That(allSnapshots.Select(s => s.SagaData.Id).Distinct().Count(), Is.EqualTo(3), "Expected three different saga IDs!");
    }

    class MessageThatCanNeverBeCorrelated { }

    class SomeSaga : Saga<SomeSagaData>, IAmInitiatedBy<string>, IHandleMessages<MessageThatCanNeverBeCorrelated>
    {
        readonly SharedCounter _sharedCounter;
        readonly bool _immediatelyMarkAsComplete;

        public SomeSaga(SharedCounter sharedCounter, bool immediatelyMarkAsComplete)
        {
            _sharedCounter = sharedCounter;
            _immediatelyMarkAsComplete = immediatelyMarkAsComplete;
        }

        protected override void CorrelateMessages(ICorrelationConfig<SomeSagaData> config)
        {
            config.Correlate<string>(GetCorrelationId, d => d.CorrelationId);

            // just be sure that this particular message can never be correlated
            config.Correlate<MessageThatCanNeverBeCorrelated>(m => Guid.NewGuid().ToString(), d => d.CorrelationId);
        }

        public async Task Handle(string message)
        {
            if (Data.CorrelationId == null) Data.CorrelationId = GetCorrelationId(message);

            Data.HandledStrings.Add(message);

            if (_immediatelyMarkAsComplete)
            {
                MarkAsComplete();
            }

            _sharedCounter.Decrement();
        }

        static string GetCorrelationId(string c)
        {
            return c.Split('/').First();
        }

        public async Task Handle(MessageThatCanNeverBeCorrelated message)
        {
        }
    }

    class SomeSagaData : ISagaData
    {
        public SomeSagaData()
        {
            HandledStrings = new HashSet<string>();
        }
        public Guid Id { get; set; }
        public int Revision { get; set; }
        public string CorrelationId { get; set; }
        public HashSet<string> HandledStrings { get; }
    }
}