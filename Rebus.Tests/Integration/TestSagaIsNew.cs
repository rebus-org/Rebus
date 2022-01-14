using System;
using System.Collections.Concurrent;
using System.Linq;
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

namespace Rebus.Tests.Integration;

[TestFixture]
public class TestSagaIsNew : FixtureBase
{
    BuiltinHandlerActivator _activator;
    IBusStarter _busStarter;

    protected override void SetUp()
    {
        _activator = Using(new BuiltinHandlerActivator());

        _busStarter = Configure.With(_activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "saga_is_new"))
            .Sagas(s => s.StoreInMemory())
            .Options(o =>
            {
                o.SetNumberOfWorkers(1);
                o.SetMaxParallelism(1);
            })
            .Create();
    }

    [Test]
    public async Task CanCorrectlyDetermineWhetherSagaInstanceIsNew()
    {
        var eventsPerCorrelationId = new ConcurrentDictionary<string, ConcurrentQueue<bool>>();

        var messages = new[]
            {
                "1/hej",
                "1/hej",
                "1/hej",

                "2/hej",
                "2/hej",

                "3/hej",

                "4/hej",
                "4/hej",
                "4/hej",
                "4/hej",
                "4/hej",
                "4/hej",
            }
            .InRandomOrder()
            .ToArray();

        var counter = new SharedCounter(messages.Length);

        Using(counter);

        _activator.Register(() => new MySaga(eventsPerCorrelationId, counter));
        _busStarter.Start();

        var bus = _activator.Bus;

        await Task.WhenAll(messages.Select(message => bus.SendLocal(message)));

        counter.WaitForResetEvent();

        Assert.That(eventsPerCorrelationId.Count, Is.EqualTo(4));

        Assert.That(eventsPerCorrelationId["1"], Is.EqualTo(new[] { true, false, false }));
        Assert.That(eventsPerCorrelationId["2"], Is.EqualTo(new[] { true, false }));
        Assert.That(eventsPerCorrelationId["3"], Is.EqualTo(new[] { true, }));
        Assert.That(eventsPerCorrelationId["4"], Is.EqualTo(new[] { true, false, false, false, false, false, }));
    }

    class MySaga : Saga<MySagaData>, IAmInitiatedBy<string>
    {
        readonly ConcurrentDictionary<string, ConcurrentQueue<bool>> _eventsPerCorrelationId;
        readonly SharedCounter _counter;

        public MySaga(ConcurrentDictionary<string, ConcurrentQueue<bool>> eventsPerCorrelationId, SharedCounter counter)
        {
            _eventsPerCorrelationId = eventsPerCorrelationId;
            _counter = counter;
        }

        protected override void CorrelateMessages(ICorrelationConfig<MySagaData> config)
        {
            config.Correlate<string>(GetCorrelationId, d => d.CorrelationId);
        }

        public async Task Handle(string message)
        {
            Data.CorrelationId = GetCorrelationId(message);

            var events = _eventsPerCorrelationId
                .GetOrAdd(Data.CorrelationId, key => new ConcurrentQueue<bool>());

            events.Enqueue(IsNew);

            _counter.Decrement();
        }

        static string GetCorrelationId(string s)
        {
            return s.Split('/').First();
        }
    }

    class MySagaData : ISagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }
        public string CorrelationId { get; set; }
    }
}