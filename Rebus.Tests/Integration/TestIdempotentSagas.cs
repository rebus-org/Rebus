using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Persistence.InMem;
using Rebus.Sagas;
using Rebus.Sagas.Idempotent;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Integration;

[TestFixture]
public class TestIdempotentSagas : FixtureBase
{
    const int MakeEveryFifthMessageFail = 5;
    BuiltinHandlerActivator _activator;
    ConcurrentDictionary<Guid, ISagaData> _persistentSagaData;
    IBusStarter _busStarter;

    protected override void SetUp()
    {
        _activator = Using(new BuiltinHandlerActivator());

        _persistentSagaData = new ConcurrentDictionary<Guid, ISagaData>();

        _busStarter = Configure.With(_activator)
            .Logging(l => l.Console(LogLevel.Info))
            .Transport(t =>
            {
                t.UseInMemoryTransport(new InMemNetwork(), "bimse");
                t.Decorate(c =>
                {
                    var transport = c.Get<ITransport>();

                    return new IntroducerOfTransportInstability(transport, MakeEveryFifthMessageFail);
                });
            })
            .Sagas(s =>
            {
                s.Decorate(c =>
                {
                    var sagaStorage = c.Get<ISagaStorage>();

                    return new SagaStorageTap(sagaStorage, _persistentSagaData);
                });

                s.StoreInMemory();
            })
            .Options(o =>
            {
                o.EnableIdempotentSagas();
            })
            .Create();
    }

    [TestCase(10)]
    public async Task ItWorks(int total)
    {
        if (total < MakeEveryFifthMessageFail)
        {
            Assert.Fail("Fail factor must be less than or equal to total!");
        }

        var allMessagesReceived = new ManualResetEvent(false);
        var receivedMessages = new ConcurrentQueue<OutgoingMessage>();

        _activator.Register((b, context) => new MyIdempotentSaga(allMessagesReceived, b));
        _activator.Register(() => new OutgoingMessageCollector(receivedMessages));

        _busStarter.Start();

        var bus = _activator.Bus;

        var messagesToSend = Enumerable
            .Range(0, total)
            .Select(id => new MyMessage
            {
                CorrelationId = "hej",
                Id = id,
                Total = total,
                SendOutgoingMessage = id % 2 == 0
            })
            .ToList();

        await Task.WhenAll(messagesToSend.Select(message => bus.SendLocal(message)));

        allMessagesReceived.WaitOrDie(TimeSpan.FromSeconds(10));

        Console.WriteLine("All messages processed by the saga - waiting for messages in outgoing message collector...");

        await Task.Delay(1000);

        var allIdempotentSagaData = _persistentSagaData.Values
            .OfType<MyIdempotentSagaData>()
            .ToList();

        Assert.That(allIdempotentSagaData.Count, Is.EqualTo(1));

        var instance = allIdempotentSagaData.First();

        Assert.That(instance.CountPerId.Count, Is.EqualTo(total));

        Assert.That(instance.CountPerId.All(c => c.Value == 1), Is.True,
            "Not all counts were exactly one: {0} - this is a sign that the saga was not truly idempotent, as the redelivery should have been caught!",
            string.Join(", ", instance.CountPerId.Where(c => c.Value > 1).Select(c => $"{c.Key}: {c.Value}")));

        var outgoingMessagesById = receivedMessages.GroupBy(m => m.Id).ToList();

        Assert.That(outgoingMessagesById.Count, Is.EqualTo(total / 2));

        Assert.That(outgoingMessagesById.Select(g => g.Key).OrderBy(id => id).ToArray(),
            Is.EqualTo(Enumerable.Range(0, total).Where(id => id % 2 == 0).ToArray()),
            "Didn't get the expected outgoing messages - expected an outgoing message for each ID whose mod 2 is zero");
    }

    class SagaStorageTap : ISagaStorage
    {
        readonly ISagaStorage _innerSagaStorage;
        readonly ConcurrentDictionary<Guid, ISagaData> _persistentSagaData;

        public SagaStorageTap(ISagaStorage innerSagaStorage, ConcurrentDictionary<Guid, ISagaData> persistentSagaData)
        {
            _innerSagaStorage = innerSagaStorage;
            _persistentSagaData = persistentSagaData;
        }

        public async Task<ISagaData> Find(Type sagaDataType, string propertyName, object propertyValue)
        {
            return await _innerSagaStorage.Find(sagaDataType, propertyName, propertyValue);
        }

        public async Task Insert(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            await _innerSagaStorage.Insert(sagaData, correlationProperties);

            _persistentSagaData[sagaData.Id] = sagaData;
        }

        public async Task Update(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            await _innerSagaStorage.Update(sagaData, correlationProperties);

            _persistentSagaData[sagaData.Id] = sagaData;
        }

        public async Task Delete(ISagaData sagaData)
        {
            await _innerSagaStorage.Delete(sagaData);

            ISagaData _;
            _persistentSagaData.TryRemove(sagaData.Id, out _);
        }
    }

    class OutgoingMessageCollector : IHandleMessages<OutgoingMessage>
    {
        readonly ConcurrentQueue<OutgoingMessage> _receivedMessages;

        public OutgoingMessageCollector(ConcurrentQueue<OutgoingMessage> receivedMessages)
        {
            _receivedMessages = receivedMessages;
        }

        public async Task Handle(OutgoingMessage message)
        {
            _receivedMessages.Enqueue(message);
        }
    }

    class MyIdempotentSaga : IdempotentSaga<MyIdempotentSagaData>, IAmInitiatedBy<MyMessage>
    {
        readonly ManualResetEvent _allMessagesReceived;
        readonly IBus _bus;

        public MyIdempotentSaga(ManualResetEvent allMessagesReceived, IBus bus)
        {
            _allMessagesReceived = allMessagesReceived;
            _bus = bus;
        }

        protected override void CorrelateMessages(ICorrelationConfig<MyIdempotentSagaData> config)
        {
            config.Correlate<MyMessage>(m => m.CorrelationId, d => d.CorrelationId);
        }

        public async Task Handle(MyMessage message)
        {
            Data.CorrelationId = message.CorrelationId;

            if (!Data.CountPerId.ContainsKey(message.Id))
            {
                Data.CountPerId[message.Id] = 0;
            }

            Data.CountPerId[message.Id]++;

            if (message.SendOutgoingMessage)
            {
                await _bus.SendLocal(new OutgoingMessage { Id = message.Id });
            }

            if (Data.CountPerId.Count == message.Total)
            {
                _allMessagesReceived.Set();
            }
        }
    }

    class MyIdempotentSagaData : IdempotentSagaData
    {
        public MyIdempotentSagaData()
        {
            CountPerId = new Dictionary<int, int>();
        }

        public string CorrelationId { get; set; }

        public Dictionary<int, int> CountPerId { get; }
    }

    class MyMessage
    {
        public string CorrelationId { get; set; }
        public int Id { get; set; }
        public int Total { get; set; }
        public bool SendOutgoingMessage { get; set; }
        public override string ToString()
        {
            return $"MyMessage {Id}/{Total}";
        }
    }

    class OutgoingMessage
    {
        public int Id { get; set; }
    }

    class IntroducerOfTransportInstability : ITransport
    {
        readonly ITransport _innerTransport;
        readonly int _failFactor;
        int _failCounter;

        public IntroducerOfTransportInstability(ITransport innerTransport, int failFactor)
        {
            _innerTransport = innerTransport;
            _failFactor = failFactor;
        }

        public void CreateQueue(string address)
        {
            _innerTransport.CreateQueue(address);
        }

        public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            await _innerTransport.Send(destinationAddress, message, context);
        }

        public async Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
        {
            var transportMessage = await _innerTransport.Receive(context, cancellationToken);
            if (transportMessage == null) return null;

            var shouldFailThisTime = Interlocked.Increment(ref _failCounter) % _failFactor == 0;

            if (shouldFailThisTime)
            {
                context.OnCommitted(async _ => throw new Exception("oh noes!!!!!"));
            }

            return transportMessage;
        }

        public string Address => _innerTransport.Address;
    }
}