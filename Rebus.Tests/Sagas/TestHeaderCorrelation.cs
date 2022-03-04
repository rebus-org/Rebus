using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Sagas;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Sagas;

[TestFixture]
public class TestHeaderCorrelation : FixtureBase
{
    BuiltinHandlerActivator _activator;
    IBusStarter _busStarter;

    protected override void SetUp()
    {
        _activator = new BuiltinHandlerActivator();

        Using(_activator);

        _busStarter = Configure.With(_activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "header-correlation"))
            .Sagas(s => s.StoreInMemory())
            .Create();
    }

    [Test]
    public async Task CanCorrelateWithHeadersOfIncomingMessages()
    {
        var sagaDataCounters = new ConcurrentDictionary<Guid, int>();
        var sharedCounter = new SharedCounter(5);

        _activator.Register(() => new MySaga(sharedCounter, sagaDataCounters, false));

        _busStarter.Start();

        var bus = _activator.Bus;

        var sameMessage = new MyMessage();
        var headers1 = new Dictionary<string, string> { { "custom-correlation-id", "saga1" } };
        var headers2 = new Dictionary<string, string> { { "custom-correlation-id", "saga2" } };

        await bus.SendLocal(sameMessage, headers1);
        await bus.SendLocal(sameMessage, headers2);
        await bus.SendLocal(sameMessage, headers2);
        await bus.SendLocal(sameMessage, headers1);
        await bus.SendLocal(sameMessage, headers1);

        sharedCounter.WaitForResetEvent(timeoutSeconds: 2);
    }

    [Test]
    public async Task CanCorrelateWithHeadersOfIncomingMessagesByUsingContext()
    {
        var sagaDataCounters = new ConcurrentDictionary<Guid, int>();
        var sharedCounter = new SharedCounter(5);

        _activator.Register(() => new MySaga(sharedCounter, sagaDataCounters, true));

        _busStarter.Start();

        var bus = _activator.Bus;

        var sameMessage = new MyMessage();
        var headers1 = new Dictionary<string, string> { { "custom-correlation-id", "saga1" } };
        var headers2 = new Dictionary<string, string> { { "custom-correlation-id", "saga2" } };

        await bus.SendLocal(sameMessage, headers1);
        await bus.SendLocal(sameMessage, headers2);
        await bus.SendLocal(sameMessage, headers2);
        await bus.SendLocal(sameMessage, headers1);
        await bus.SendLocal(sameMessage, headers1);

        sharedCounter.WaitForResetEvent(timeoutSeconds: 2);
    }

    class MyMessage { }

    class MySaga : Saga<MySagaData>, IAmInitiatedBy<MyMessage>
    {
        readonly SharedCounter _sharedCounter;
        readonly ConcurrentDictionary<Guid, int> _sagaDataCounters;
        readonly bool _useMessageContext;

        public MySaga(SharedCounter sharedCounter, ConcurrentDictionary<Guid, int> sagaDataCounters, bool useMessageContext)
        {
            _sharedCounter = sharedCounter;
            _sagaDataCounters = sagaDataCounters;
            _useMessageContext = useMessageContext;
        }

        protected override void CorrelateMessages(ICorrelationConfig<MySagaData> config)
        {
            if (_useMessageContext)
            {
                config.CorrelateContext<MyMessage>(context => context.Headers["custom-correlation-id"], d => d.CorrelationId);
            }
            else
            {
                config.CorrelateHeader<MyMessage>("custom-correlation-id", d => d.CorrelationId);
            }
        }

        public async Task Handle(MyMessage message)
        {
            await Task.FromResult(false);

            _sagaDataCounters.AddOrUpdate(Data.Id, 1, (_, count) => count + 1);
            _sharedCounter.Decrement();
        }
    }

    class MySagaData : SagaData
    {
        public string CorrelationId { get; set; }
    }
}