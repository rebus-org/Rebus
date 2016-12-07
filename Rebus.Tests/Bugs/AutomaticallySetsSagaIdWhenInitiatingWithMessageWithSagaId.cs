using System;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Sagas;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Bugs
{
    public class AutomaticallySetsSagaIdWhenInitiatingWithMessageWithSagaId : FixtureBase
    {
        BuiltinHandlerActivator _activator;

        public AutomaticallySetsSagaIdWhenInitiatingWithMessageWithSagaId()
        {
            _activator = Using(new BuiltinHandlerActivator());

            Configure.With(_activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "saga-id-is-preserved"))
                .Start();
        }

        [Fact]
        public void ItHasBeenFixed()
        {
            var counter = new SharedCounter(1);
            var fields = new Fields();

            _activator.Register(() => new MySaga(counter, fields));

            var newGuid = Guid.NewGuid();
            _activator.Bus.SendLocal(new MyMessageWithGuid {SagaId = newGuid}).Wait();

            counter.WaitForResetEvent();

            Assert.Equal(fields.SagaIdPropertyFromMessage, fields.InitialSagaId);
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