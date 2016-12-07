using System;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
using Xunit;

namespace Rebus.Tests.Sagas.TestIdCorrelation
{
    public class Scenario1 : FixtureBase
    {
        readonly BuiltinHandlerActivator _activator;
        readonly InMemorySagaStorage _sagas;

        public Scenario1()
        {
            _activator = Using(new BuiltinHandlerActivator());

            _sagas = new InMemorySagaStorage();

            Configure.With(_activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "saga-id-correlation"))
                .Sagas(s => s.Register(c => _sagas))
                .Start();
        }

        [Fact]
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

            Assert.Equal(2, sagaInstances.Count);
        }
    }
}