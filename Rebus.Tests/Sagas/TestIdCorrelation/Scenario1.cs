using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Sagas.TestIdCorrelation
{
    [TestFixture]
    public class Scenario1 : FixtureBase
    {
        BuiltinHandlerActivator _activator;
        InMemorySagaStorage _sagas;

        protected override void SetUp()
        {
            _activator = Using(new BuiltinHandlerActivator());

            _sagas = new InMemorySagaStorage();

            Configure.With(_activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "saga-id-correlation"))
                .Sagas(s => s.Register(c => _sagas))
                .Start();
        }

        [Test]
        public async Task CanInitiateSagaAndOverrideItsId()
        {
            var counter = new SharedCounter(5);

            _activator.Register(() => new DefaultSaga(counter));

            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            // five messages using 2 ids
            await _activator.Bus.SendLocal(new DefaultSagaMessage(id1));
            await _activator.Bus.SendLocal(new DefaultSagaMessage(id2));
            await _activator.Bus.SendLocal(new DefaultSagaMessage(id1));
            await _activator.Bus.SendLocal(new DefaultSagaMessage(id2));
            await _activator.Bus.SendLocal(new DefaultSagaMessage(id1));

            counter.WaitForResetEvent();

            var sagaInstances = _sagas.Instances.ToList();

            Assert.That(sagaInstances.Count, Is.EqualTo(2));
        }
    }
}