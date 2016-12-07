using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Auditing.Sagas;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Logging;
using Rebus.Sagas;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Contracts.Sagas
{
    public class SagaSnapshotStorageTest<TFactory> : FixtureBase where TFactory : ISagaSnapshotStorageFactory, new()
    {
        BuiltinHandlerActivator _activator;
        IBus _bus;
        TFactory _factory;
        ListLoggerFactory _logger;

        public SagaSnapshotStorageTest()
        {
            _factory = new TFactory();

            _activator = new BuiltinHandlerActivator();

            Using(_activator);

            _logger = new ListLoggerFactory(true);

            _bus = Configure.With(_activator)
                .Logging(l => l.Use(_logger))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "saga_snapshots_integration_testerino"))
                .Options(o => o.EnableSagaAuditing().Register(c => _factory.Create()))
                .Start();
        }

        [Fact]
        public async Task DoesNotFailWhenSagaDataCouldNotBeFound()
        {
            var sharedCounter = Using(new SharedCounter(1));

            _activator.Register(() => new SomeSaga(sharedCounter, false));

            await _bus.SendLocal(new MessageThatCanNeverBeCorrelated());

            await Task.Delay(2000);

            var lines = _logger.ToList();

            var warningsOrAbove = lines.Where(l => l.Level >= LogLevel.Warn).ToList();

            Assert.False(warningsOrAbove.Any(),
                $@"Didn't expect warnings or errors, but got this:

{string.Join(Environment.NewLine, warningsOrAbove)}

as part of this:

{string.Join(Environment.NewLine, lines)}");
        }

        [Fact]
        public void CreatesSnapshotOfSagaData()
        {
            var sharedCounter = new SharedCounter(3, "Message counter")
            {
                Delay = TimeSpan.FromSeconds(2)
            };

            Using(sharedCounter);

            _activator.Register(() => new SomeSaga(sharedCounter, false));

            Task.WaitAll(
                _bus.SendLocal("hej/med dig"),
                _bus.SendLocal("hej/igen"),
                _bus.SendLocal("hej/igen med dig")
                );

            sharedCounter.WaitForResetEvent();

            var allSnapshots = _factory.GetAllSnapshots().ToList();

            Assert.Equal(3, allSnapshots.Count);

            // "All snapshots should have the same saga ID!"
            Assert.All(allSnapshots, s => Assert.Equal(allSnapshots.First().SagaData.Id, s.SagaData.Id));

            // "Expect the three initial revisions"
            Assert.Equal(new[] { 0, 1, 2 }, allSnapshots.OrderBy(s => s.SagaData.Revision).Select(s => s.SagaData.Revision));
        }

        [Fact]
        public void CreatesSnapshotOfSagaDataAlsoWhenImmediatelyMarkingAsComplete()
        {
            var sharedCounter = new SharedCounter(3, "Message counter")
            {
                Delay = TimeSpan.FromSeconds(1)
            };

            Using(sharedCounter);

            _activator.Register(() => new SomeSaga(sharedCounter, true));

            Task.WaitAll(
                _bus.SendLocal("hej/med dig"),
                _bus.SendLocal("hej/igen"),
                _bus.SendLocal("hej/igen med dig")
                );

            sharedCounter.WaitForResetEvent();

            Thread.Sleep(500); //< be sure that the snapshot has made it to the database

            var allSnapshots = _factory.GetAllSnapshots().ToList();

            Assert.Equal(3, allSnapshots.Count);

            // "All revisions should be initial!
            Assert.All(allSnapshots, snapshot => Assert.Equal(0, snapshot.SagaData.Revision));

            //"Expect three different saga IDs!"
            Assert.Equal(3, allSnapshots.Select(s => s.SagaData.Id).Distinct().Count());
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

}