using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Sagas;
using Rebus.Sagas.Idempotent;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Integration
{
    public class TestIdempotentSagas : FixtureBase
    {
        const int MakeEveryFifthMessageFail = 5;
        readonly BuiltinHandlerActivator _activator;
        readonly IBus _bus;
        readonly ConcurrentDictionary<Guid, ISagaData> _persistentSagaData;

        public TestIdempotentSagas()
        {
            _activator = Using(new BuiltinHandlerActivator());

            _persistentSagaData = new ConcurrentDictionary<Guid, ISagaData>();

            _bus = Configure.With(_activator)
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
                })
                .Options(o =>
                {
                    o.EnableIdempotentSagas();
                })
                .Start();
        }

        [Theory]
        [InlineData(10)]
        public async Task ItWorks(int total)
        {
            if (total < MakeEveryFifthMessageFail)
            {
                Assert.True(false,"Fail factor must be less than or equal to total!");
            }

            var allMessagesReceived = new ManualResetEvent(false);
            var receivedMessages = new ConcurrentQueue<OutgoingMessage>();

            _activator.Register((b, context) => new MyIdempotentSaga(allMessagesReceived, b));
            _activator.Register(() => new OutgoingMessageCollector(receivedMessages));

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

            await Task.WhenAll(messagesToSend.Select(message => _bus.SendLocal(message)));

            allMessagesReceived.WaitOrDie(TimeSpan.FromSeconds(10));

            Console.WriteLine("All messages processed by the saga - waiting for messages in outgoing message collector...");

            await Task.Delay(1000);

            var allIdempotentSagaData = _persistentSagaData.Values
                .OfType<MyIdempotentSagaData>()
                .ToList();

            Assert.Equal(1, allIdempotentSagaData.Count);

            var instance = allIdempotentSagaData.First();

            Assert.Equal(total, instance.CountPerId.Count);

            Assert.True(instance.CountPerId.All(c => c.Value == 1),
                $"Not all counts were exactly one: {string.Join(", ", instance.CountPerId.Where(c => c.Value > 1).Select(c => $"{c.Key}: {c.Value}"))} " +
                $"- this is a sign that the saga was not truly idempotent, as the redelivery should have been caught!");

            var outgoingMessagesById = receivedMessages.GroupBy(m => m.Id).ToList();

            Assert.Equal(total / 2, outgoingMessagesById.Count);

            // if it fails: "Didn't get the expected outgoing messages - expected an outgoing message for each ID whose mod 2 is zero");
            Assert.Equal(Enumerable.Range(0, total).Where(id => id % 2 == 0).ToArray(),
                outgoingMessagesById.Select(g => g.Key).OrderBy(id => id).ToArray());
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
                    context.OnCommitted(async () =>
                    {
                        throw new Exception("oh noes!!!!!");
                    });
                }

                return transportMessage;
            }

            public string Address
            {
                get { return _innerTransport.Address; }
            }
        }
    }
}