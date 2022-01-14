using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Messages;
using Rebus.Persistence.InMem;
using Rebus.Sagas;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Integration;

[TestFixture]
public class TestSaga : FixtureBase
{
    BuiltinHandlerActivator _handlerActivator;
    IBus _bus;
    List<string> _recordedCalls;

    protected override void SetUp()
    {
        _recordedCalls = new List<string>();
        _handlerActivator = new BuiltinHandlerActivator();
        _handlerActivator.Register(() => new MySaga(_recordedCalls));

        _bus = Configure.With(_handlerActivator)
            .Logging(l => l.Console())
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(true), "test.sagas.input"))
            .Options(o =>
            {
                o.SetNumberOfWorkers(1);
                o.SetMaxParallelism(1);
            })
            .Sagas(s => s.StoreInMemory())
            .Start();

        Using(_bus);
    }

    protected override void TearDown()
    {
        CleanUpDisposables();
    }

    [Test]
    public async Task CanHitSaga()
    {
        // initiate three saga instances
        await Task.WhenAll(
            _bus.SendLocal(new InitiatingMessage { CorrelationId = "saga1" }, Id("init1_1")),
            _bus.SendLocal(new InitiatingMessage { CorrelationId = "saga2" }, Id("init2_2")),
            _bus.SendLocal(new InitiatingMessage { CorrelationId = "saga3" }, Id("init3_3"))
        );

        // do some stuff to the sagas
        await Task.WhenAll(
            _bus.SendLocal(new CorrelatedMessage { CorrelationId = "saga1" }, Id("corr1_4")),
            _bus.SendLocal(new CorrelatedMessage { CorrelationId = "saga1" }, Id("corr1_5")),
            _bus.SendLocal(new CorrelatedMessage { CorrelationId = "saga1" }, Id("corr1_6")),

            _bus.SendLocal(new CorrelatedMessage { CorrelationId = "saga2" }, Id("corr2_7")),
            _bus.SendLocal(new CorrelatedMessage { CorrelationId = "saga2" }, Id("corr2_8")),

            _bus.SendLocal(new CorrelatedMessage { CorrelationId = "saga3" }, Id("corr3_9")),

            _bus.SendLocal(new CorrelatedMessage { CorrelationId = "saga4" }, Id("corr4_10"))
        );

        await Task.Delay(2000);

        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine(" Recorded calls");
        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine(string.Join(Environment.NewLine, _recordedCalls));
        Console.WriteLine("----------------------------------------------------------------");

        Assert.That(_recordedCalls.Count, Is.EqualTo(9));
            
        Assert.That(_recordedCalls.Where(c => c.StartsWith("saga1:")).ToArray(), Is.EqualTo(new[]
        {
            "saga1:InitiatingMessage",
            "saga1:CorrelatedMessage",
            "saga1:CorrelatedMessage",
            "saga1:CorrelatedMessage",
        }));

        Assert.That(_recordedCalls.Where(c => c.StartsWith("saga2:")).ToArray(), Is.EqualTo(new[]
        {
            "saga2:InitiatingMessage",
            "saga2:CorrelatedMessage",
            "saga2:CorrelatedMessage",
        }));

        Assert.That(_recordedCalls.Where(c => c.StartsWith("saga3:")).ToArray(), Is.EqualTo(new[]
        {
            "saga3:InitiatingMessage",
            "saga3:CorrelatedMessage",
        }));
    }

    static Dictionary<string, string> Id(string id)
    {
        return new Dictionary<string, string> { { Headers.MessageId, id } };
    }

    class InitiatingMessage
    {
        public string CorrelationId { get; set; }
    }

    class CorrelatedMessage
    {
        public string CorrelationId { get; set; }
    }

    class MySaga : Saga<MySagaData>, IAmInitiatedBy<InitiatingMessage>, IHandleMessages<CorrelatedMessage>
    {
        readonly List<string> _recordedCalls;

        public MySaga(List<string> recordedCalls)
        {
            _recordedCalls = recordedCalls;
        }

        protected override void CorrelateMessages(ICorrelationConfig<MySagaData> config)
        {
            config.Correlate<InitiatingMessage>(m => m.CorrelationId, d => d.CorrelationId);
            config.Correlate<CorrelatedMessage>(m => m.CorrelationId, d => d.CorrelationId);
        }

        public async Task Handle(InitiatingMessage message)
        {
            Data.CorrelationId = message.CorrelationId;

            Increment(message.GetType(), message.CorrelationId);
        }

        public async Task Handle(CorrelatedMessage message)
        {
            Increment(message.GetType(), message.CorrelationId);
        }

        void Increment(Type type, string correlationId)
        {
            if (!Data.ProcessedMessages.ContainsKey(type))
                Data.ProcessedMessages[type] = 0;

            Data.ProcessedMessages[type]++;

            _recordedCalls.Add($"{correlationId}:{type.Name}");
        }
    }

    class MySagaData : ISagaData
    {
        public MySagaData()
        {
            ProcessedMessages = new Dictionary<Type, int>();
        }

        public Guid Id { get; set; }
        public int Revision { get; set; }

        public string CorrelationId { get; set; }

        public Dictionary<Type, int> ProcessedMessages { get; }
    }
}