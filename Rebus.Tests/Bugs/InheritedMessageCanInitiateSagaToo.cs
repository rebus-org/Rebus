using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Sagas;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Bugs;

[TestFixture]
public class InheritedMessageCanInitiateSagaToo : FixtureBase
{
    BuiltinHandlerActivator _activator;
    IBusStarter _busStarter;

    protected override void SetUp()
    {
        _activator = Using(new BuiltinHandlerActivator());

        _busStarter = Configure.With(_activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "inherited-message-can-be-an-initiating-message-too"))
            .Sagas(s => s.StoreInMemory())
            .Options(o =>
            {
                o.SetNumberOfWorkers(1);
                o.SetMaxParallelism(1);
            })
            .Create();
    }

    [Test]
    public void CanCorrelateWithIncomingMessageWhichIsInherited()
    {
        var counter = new SharedCounter(1);

        _activator.Register((bus, context) => new PolySaga(counter));

        _busStarter.Start();

        _activator.Bus.SendLocal(new ConcreteInitiatingMessage()).Wait();

        counter.WaitForResetEvent(timeoutSeconds: 2);
    }

    class PolySaga : Saga<PolySagaState>, IAmInitiatedBy<AbstractInitiatingMessage>
    {
        readonly SharedCounter _counter;

        public PolySaga(SharedCounter counter)
        {
            _counter = counter;
        }

        protected override void CorrelateMessages(ICorrelationConfig<PolySagaState> config)
        {
            config.Correlate<AbstractInitiatingMessage>(m => Guid.NewGuid(), d => d.Id);
        }

        public async Task Handle(AbstractInitiatingMessage message)
        {
            _counter.Decrement();
        }
    }

    class PolySagaState : ISagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }
    }

    abstract class AbstractInitiatingMessage { }

    class ConcreteInitiatingMessage : AbstractInitiatingMessage { }
}