using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Sagas;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestSagasAndPolymorphicCorrelation : FixtureBase
    {
        BuiltinHandlerActivator _activator;

        protected override void SetUp()
        {
            _activator = Using(new BuiltinHandlerActivator());

            Configure.With(_activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "polycorrrewwllll"))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(1);
                    o.SetMaxParallelism(1);
                })
                .Start();
        }

        [Test]
        public void CanCorrelateWithIncomingMessageWhichIsInherited()
        {
            var encounteredSagaIds = new ConcurrentQueue<Guid>();
            var counter = new SharedCounter(4);

            _activator.Register((bus, context) => new PolySaga(encounteredSagaIds, counter));

            _activator.Bus.SendLocal(new ConcretePolyMessage("blah!")).Wait();
            _activator.Bus.SendLocal(new ConcretePolyMessage("blah!")).Wait();
            _activator.Bus.SendLocal(new ConcretePolyMessage("blah!")).Wait();
            _activator.Bus.SendLocal(new ConcretePolyMessage("blah!")).Wait();

            counter.WaitForResetEvent();

            Assert.That(encounteredSagaIds.Distinct().Count(), Is.EqualTo(1));
        }

        class PolySaga : Saga<PolySagaState>, IAmInitiatedBy<AbstractPolyMessage>
        {
            readonly ConcurrentQueue<Guid> _sagaIdsEncountered;
            readonly SharedCounter _counter;

            public PolySaga(ConcurrentQueue<Guid> sagaIdsEncountered, SharedCounter counter)
            {
                _sagaIdsEncountered = sagaIdsEncountered;
                _counter = counter;
            }

            protected override void CorrelateMessages(ICorrelationConfig<PolySagaState> config)
            {
                config.Correlate<AbstractPolyMessage>(m => m.CorrelationId, d => d.CorrelationId);
            }

            public async Task Handle(AbstractPolyMessage message)
            {
                Data.CorrelationId = message.CorrelationId;

                _sagaIdsEncountered.Enqueue(Data.Id);

                _counter.Decrement();
            }
        }

        class PolySagaState : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
            public string CorrelationId { get; set; }
        }

        abstract class AbstractPolyMessage
        {
            protected AbstractPolyMessage(string correlationId)
            {
                CorrelationId = correlationId;
            }

            public string CorrelationId { get; }
        }

        class ConcretePolyMessage : AbstractPolyMessage
        {
            public ConcretePolyMessage(string correlationId) : base(correlationId)
            {
            }
        }
    }
}