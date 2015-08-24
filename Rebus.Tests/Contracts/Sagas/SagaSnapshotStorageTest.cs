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
using Rebus.Sagas;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Contracts.Sagas
{
    public class SagaSnapshotStorageTest<TFactory> : FixtureBase where TFactory : ISagaSnapshotStorageFactory, new()
    {
        BuiltinHandlerActivator _activator;
        IBus _bus;
        TFactory _factory;

        protected override void SetUp()
        {
            _factory = new TFactory();

            _activator = Using(new BuiltinHandlerActivator());

            _bus = Configure.With(_activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "saga_snapshots_integration_testerino"))
                .Options(o => o.EnableSagaAuditing().Register(c => _factory.Create()))
                .Start();
        }

        [Test]
        public void CreatesSnapshotOfSagaData()
        {
            var sharedCounter = new SharedCounter(3, "Message counter")
            {
                Delay = TimeSpan.FromSeconds(0.5)
            };

            _activator.Register(() => new SomeSaga(sharedCounter, false));

            Task.WaitAll(
                _bus.SendLocal("hej/med dig"),
                _bus.SendLocal("hej/igen"),
                _bus.SendLocal("hej/igen med dig")
                );

            sharedCounter.WaitForResetEvent();

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
                Delay = TimeSpan.FromSeconds(0.5)
            };

            _activator.Register(() => new SomeSaga(sharedCounter, true));

            Task.WaitAll(
                _bus.SendLocal("hej/med dig"),
                _bus.SendLocal("hej/igen"),
                _bus.SendLocal("hej/igen med dig")
                );

            sharedCounter.WaitForResetEvent();

            var allSnapshots = _factory.GetAllSnapshots().ToList();

            Assert.That(allSnapshots.Count, Is.EqualTo(3));
            
            Assert.That(allSnapshots.All(s => s.SagaData.Revision == 0), "Not all revisions were initial!: {0}", allSnapshots.Select(s => s.SagaData.Revision));
            
            Assert.That(allSnapshots.Select(s => s.SagaData.Id).Distinct().Count(), Is.EqualTo(3), "Expected three different saga IDs!");
        }

        class SomeSaga : Saga<SomeSagaData>, IAmInitiatedBy<string>
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
            public HashSet<string> HandledStrings { get; private set; }
        }
    }

}