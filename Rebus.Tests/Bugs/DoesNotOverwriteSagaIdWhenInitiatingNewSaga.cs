using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Sagas;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Bugs
{
    [TestFixture]
    public class DoesNotOverwriteSagaIdWhenInitiatingNewSaga : FixtureBase
    {
        BuiltinHandlerActivator _activator;

        protected override void SetUp()
        {
            _activator = Using(new BuiltinHandlerActivator());

            Configure.With(_activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "saga-id-is-preserved"))
                .Start();
        }

        [Test]
        public void ItHasBeenFixed()
        {
            var counter = new SharedCounter(1);
            var fields = new Fields();

            _activator.Register(() => new MySaga(counter, fields));

            var newGuid = Guid.NewGuid();
            _activator.Bus.SendLocal(new MyMessageWithGuid {SagaId = newGuid}).Wait();

            counter.WaitForResetEvent();

            Assert.That(fields.InitialSagaId, Is.Not.EqualTo(fields.SagaIdPropertyFromMessage));
        }

        class Fields
        {
            public Guid InitialSagaId { get; set; }
            public Guid SagaIdPropertyFromMessage { get; set; }
        }

        class MySaga : Saga<MySagaState>, IAmInitiatedBy<MyMessageWithGuid>
        {
            readonly SharedCounter _counter;
            readonly Fields _fields;

            public MySaga(SharedCounter counter, Fields fields)
            {
                _counter = counter;
                _fields = fields;
            }

            protected override void CorrelateMessages(ICorrelationConfig<MySagaState> config)
            {
                // correlate with saga ID
                config.Correlate<MyMessageWithGuid>(m => m.SagaId, d => d.Id);
            }

            public async Task Handle(MyMessageWithGuid message)
            {
                _fields.SagaIdPropertyFromMessage = message.SagaId;
                _fields.InitialSagaId = Data.Id;

                _counter.Decrement();
            }
        }

        class MySagaState : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
        }

        class MyMessageWithGuid
        {
            public Guid SagaId { get; set; }
        }
    }

}