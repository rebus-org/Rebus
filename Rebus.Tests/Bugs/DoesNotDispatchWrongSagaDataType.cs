using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Sagas;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Bugs;

[TestFixture]
[Description("Two sagas, each with its own saga data type. One message to hit them both. And it turned out that it didn't explode - it actually seemed to work fine. I wonder........")]
public class DoesNotDispatchWrongSagaDataType : FixtureBase
{
    BuiltinHandlerActivator _activator;
    IBusStarter _busStarter;

    protected override void SetUp()
    {
        _activator = Using(new BuiltinHandlerActivator());

        _busStarter = Configure.With(_activator)
            .Logging(l => l.Use(new ListLoggerFactory(true)))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "doesntmatter"))
            .Sagas(s => s.StoreInMemory())
            .Options(o =>
            {
                o.SetNumberOfWorkers(1);
                o.SetMaxParallelism(1);
            })
            .Create();
    }

    [Test]
    public void DoesNotBreak()
    {
        var counter = new SharedCounter(6);
            
        _activator.Register(() => new FirstSaga(counter));
        _activator.Register(() => new SecondSaga(counter));

        _busStarter.Start();

        var sagaId = Guid.NewGuid().ToString();

        3.Times(() => _activator.Bus.SendLocal(new Message(sagaId)).Wait());

        counter.WaitForResetEvent(3);
    }

    class FirstSagaData : ISagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }
        public string CorrelationId { get; set; }
    }

    class SecondSagaData : ISagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }
        public string CorrelationId { get; set; }
    }

    class Message
    {
        public Message(string correlationId) { CorrelationId = correlationId; }
        public string CorrelationId { get; }
    }

    class FirstSaga : Saga<FirstSagaData>, IAmInitiatedBy<Message>
    {
        readonly SharedCounter _counter;

        public FirstSaga(SharedCounter counter)
        {
            _counter = counter;
        }

        protected override void CorrelateMessages(ICorrelationConfig<FirstSagaData> config)
        {
            config.Correlate<Message>(m => m.CorrelationId, d => d.CorrelationId);
        }

        public async Task Handle(Message message)
        {
            _counter.Decrement();
        }
    }

    class SecondSaga : Saga<SecondSagaData>, IAmInitiatedBy<Message>
    {
        readonly SharedCounter _counter;

        public SecondSaga(SharedCounter counter)
        {
            _counter = counter;
        }

        protected override void CorrelateMessages(ICorrelationConfig<SecondSagaData> config)
        {
            config.Correlate<Message>(m => m.CorrelationId, d => d.CorrelationId);
        }

        public async Task Handle(Message message)
        {
            _counter.Decrement();
        }
    }
}