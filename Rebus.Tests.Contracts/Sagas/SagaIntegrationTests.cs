using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Logging;
using Rebus.Sagas;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Contracts.Sagas;

/// <summary>
/// Test fixture base class for verifying compliance with the <see cref="ISagaStorage"/> contract
/// </summary>
public abstract class SagaIntegrationTests<TFactory> : FixtureBase where TFactory : ISagaStorageFactory, new()
{
    TFactory _factory;

    protected override void SetUp()
    {
        _factory = new TFactory();
    }

    protected override void TearDown()
    {
        _factory.CleanUp();
    }

    [Test]
    public async Task DoesNotChokeWhenCorrelatingMultipleMessagesWithTheSameCorrelationProperty()
    {
        using var done = new ManualResetEvent(false);
        using var activator = new BuiltinHandlerActivator();

        activator.Register((b, _) => new MySaga(done, b));

        var bus = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "sagastuff"))
            .Options(o =>
            {
                o.SetNumberOfWorkers(1);
                o.SetMaxParallelism(1);
            })
            .Sagas(s => s.Register(_ => _factory.GetSagaStorage()))
            .Start();

        await Task.WhenAll(
            bus.SendLocal(new Message1 { CorrelationId = "bimse" }),
            bus.SendLocal(new Message2 { CorrelationId = "bimse" })
        );

        done.WaitOrDie(TimeSpan.FromSeconds(5));
    }

    class MySagaData : ISagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }
        public string CorrelationId { get; set; }

        public bool GotTheFirst { get; set; }
        public bool GotTheSecond { get; set; }
    }

    class MySaga : Saga<MySagaData>,
        IAmInitiatedBy<Message1>,
        IAmInitiatedBy<Message2>,
        IHandleMessages<Message3>
    {
        readonly ManualResetEvent _done;
        readonly IBus _bus;

        public MySaga(ManualResetEvent done, IBus bus)
        {
            _done = done;
            _bus = bus;
        }

        protected override void CorrelateMessages(ICorrelationConfig<MySagaData> config)
        {
            config.Correlate<Message1>(m => m.CorrelationId, d => d.CorrelationId);
            config.Correlate<Message2>(m => m.CorrelationId, d => d.CorrelationId);
            config.Correlate<Message3>(m => m.CorrelationId, d => d.CorrelationId);
        }

        public async Task Handle(Message1 message)
        {
            Data.CorrelationId = message.CorrelationId;
            Data.GotTheFirst = true;

            if (Complete)
            {
                await _bus.SendLocal(new Message3 { CorrelationId = Data.CorrelationId });
            }
        }

        public async Task Handle(Message2 message)
        {
            Data.CorrelationId = message.CorrelationId;
            Data.GotTheSecond = true;

            if (Complete)
            {
                await _bus.SendLocal(new Message3 { CorrelationId = Data.CorrelationId });
            }
        }

        bool Complete => Data.GotTheFirst && Data.GotTheSecond;

        public async Task Handle(Message3 message)
        {
            _done.Set();
        }
    }

    class Message1
    {
        public string CorrelationId { get; set; }
    }

    class Message2
    {
        public string CorrelationId { get; set; }
    }

    class Message3
    {
        public string CorrelationId { get; set; }
    }

    [Test]
    public async Task CanFinishSaga()
    {
        var activator = new BuiltinHandlerActivator();
        var events = new ConcurrentQueue<string>();

        activator.Register(() => new TestSaga(events, 3));

        Using(activator);

        var bus = Configure.With(activator)
            .Logging(l => l.Console(minLevel: LogLevel.Warn))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(true), "finish-saga-test"))
            .Sagas(s => s.Register(c => _factory.GetSagaStorage()))
            .Options(o =>
            {
                o.SetNumberOfWorkers(1);
                o.SetMaxParallelism(1);
            })
            .Start();

        const int millisecondsDelay = 500;

        var stopwatch = Stopwatch.StartNew();

        Console.WriteLine($"t: {stopwatch.Elapsed.TotalMilliseconds:0.#} ms");
        await bus.SendLocal(new SagaMessage { Id = 70 });
        await Task.Delay(millisecondsDelay);

        Console.WriteLine($"t: {stopwatch.Elapsed.TotalMilliseconds:0.#} ms");
        await bus.SendLocal(new SagaMessage { Id = 70 });
        await Task.Delay(millisecondsDelay);

        Console.WriteLine($"t: {stopwatch.Elapsed.TotalMilliseconds:0.#} ms");
        await bus.SendLocal(new SagaMessage { Id = 70 });
        await Task.Delay(millisecondsDelay);

        Console.WriteLine($"t: {stopwatch.Elapsed.TotalMilliseconds:0.#} ms");
        await bus.SendLocal(new SagaMessage { Id = 70 });
        await Task.Delay(millisecondsDelay);

        Console.WriteLine($"t: {stopwatch.Elapsed.TotalMilliseconds:0.#} ms");
        await bus.SendLocal(new SagaMessage { Id = 70 });
        await Task.Delay(millisecondsDelay);

        await Task.Delay(3 * millisecondsDelay);

        var expected = new[]
        {
            "70:1",
            "70:2",
            "70:3", // it is marked as completed here
            "70:1",
            "70:2",
        };

        var actual = events.ToArray();

        Console.WriteLine($"t: {stopwatch.Elapsed.TotalMilliseconds:0.#} ms");
        Assert.That(actual, Is.EqualTo(expected), $@"Received events 

{string.Join(Environment.NewLine, actual)}

did not match expected

{string.Join(Environment.NewLine, expected)}

Five events with ID = 70 are sent. The saga stamps 'events' down along with the number of
messages it has processed - e.g. 70:2 means 'Message with ID 70 handled as the 2nd message'.

The saga was supposed to receive the first three events and then mark itself as completed.
After that, the last two events should have been received.
");
    }

    class TestSaga : Saga<TestSagaData>, IAmInitiatedBy<SagaMessage>
    {
        readonly ConcurrentQueue<string> _stuff;
        readonly int _maxNumberOfProcessedMessages;

        public TestSaga(ConcurrentQueue<string> stuff, int maxNumberOfProcessedMessages)
        {
            _stuff = stuff;
            _maxNumberOfProcessedMessages = maxNumberOfProcessedMessages;
        }

        protected override void CorrelateMessages(ICorrelationConfig<TestSagaData> config)
        {
            config.Correlate<SagaMessage>(m => m.Id, d => d.CorrelationId);
        }

        public async Task Handle(SagaMessage message)
        {
            Data.CorrelationId = message.Id;
            Data.NumberOfProcessedMessages++;

            var eventString = $"{Data.CorrelationId}:{Data.NumberOfProcessedMessages}";

            Printt($"Enqueueing '{eventString}'");
                
            _stuff.Enqueue(eventString);

            if (Data.NumberOfProcessedMessages >= _maxNumberOfProcessedMessages)
            {
                MarkAsComplete();
            }
        }
    }

    class TestSagaData : ISagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }
        public int CorrelationId { get; set; }
        public int NumberOfProcessedMessages { get; set; }
    }

    class SagaMessage
    {
        public int Id { get; set; }
    }
}