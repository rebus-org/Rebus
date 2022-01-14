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
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Sagas;

[TestFixture]
public class TestMarkAsComplete : FixtureBase
{
    BuiltinHandlerActivator _activator;
    IBusStarter _busStarter;

    protected override void SetUp()
    {
        _activator = Using(new BuiltinHandlerActivator());

        _busStarter = Configure.With(_activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "test saga things"))
            .Options(o =>
            {
                o.SetMaxParallelism(1);
                o.SetNumberOfWorkers(1);
            })
            .Sagas(s => s.StoreInMemory())
            .Create();
    }

    [Test]
    public async Task CanMarkSagaAsComplete()
    {
        var registeredCounts = new ConcurrentQueue<int>();

        _activator.Register(() => new SomeSaga(registeredCounts));

        _busStarter.Start();

        await _activator.Bus.SendLocal("1/hej");
        await _activator.Bus.SendLocal("1/med");
        await _activator.Bus.SendLocal("1/dig");
        await Task.Delay(400);
        await _activator.Bus.SendLocal("1/complete!");

        await Task.Delay(400);

        await _activator.Bus.SendLocal("1/hej");

        await Task.Delay(400);

        Assert.That(registeredCounts.ToArray(), Is.EqualTo(new[] { 1, 2, 3, 4, 1 }));
    }

    class SomeSaga : Saga<SomeSagaData>, IAmInitiatedBy<string>
    {
        readonly ConcurrentQueue<int> _registeredCounts;

        public SomeSaga(ConcurrentQueue<int> registeredCounts)
        {
            _registeredCounts = registeredCounts;
        }

        protected override void CorrelateMessages(ICorrelationConfig<SomeSagaData> config)
        {
            config.Correlate<string>(GetString, d => d.String);
        }

        public async Task Handle(string message)
        {
            Data.String = GetString(message);
            Data.InvocationCount++;

            _registeredCounts.Enqueue(Data.InvocationCount);

            if (message.EndsWith("complete!"))
            {
                MarkAsComplete();
            }
        }

        static string GetString(string m)
        {
            return m.Split('/').First();
        }
    }

    class SomeSagaData : ISagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }

        public string String { get; set; }
        public int InvocationCount { get; set; }
    }
}