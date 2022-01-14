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
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Sagas;

[TestFixture]
public class TestSagaCorrelationPropertyNesting : FixtureBase
{
    [Test]
    public async Task ItWorks()
    {
        var activator = Using(new BuiltinHandlerActivator());
        var messagesBySagaId = new ConcurrentDictionary<Guid, int>();
        var counter = new SharedCounter(5);

        activator.Register(() => new MySaga(messagesBySagaId, counter));

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "saga-property-nesting"))
            .Sagas(s => s.StoreInMemory())
            .Start();

        await activator.Bus.SendLocal(new MyMsg("hej1"));
        await activator.Bus.SendLocal(new MyMsg("hej1"));
        await activator.Bus.SendLocal(new MyMsg("hej2"));
        await activator.Bus.SendLocal(new MyMsg("hej2"));
        await activator.Bus.SendLocal(new MyMsg("hej2"));

        counter.WaitForResetEvent();

        Console.WriteLine(string.Join(Environment.NewLine, messagesBySagaId));

        Assert.That(messagesBySagaId.Count, Is.EqualTo(2));
        Assert.That(messagesBySagaId.Count(kvp => kvp.Value == 2), Is.EqualTo(1));
        Assert.That(messagesBySagaId.Count(kvp => kvp.Value == 3), Is.EqualTo(1));
    }

    class MySaga : Saga<MySagaData>, IAmInitiatedBy<MyMsg>
    {
        readonly ConcurrentDictionary<Guid, int> _messagesBySagaId;
        readonly SharedCounter _counter;

        public MySaga(ConcurrentDictionary<Guid, int> messagesBySagaId, SharedCounter counter)
        {
            _messagesBySagaId = messagesBySagaId;
            _counter = counter;
        }

        protected override void CorrelateMessages(ICorrelationConfig<MySagaData> config)
        {
            config.Correlate<MyMsg>(m => m.CorrId, d => d.Child.Property);
        }

        public async Task Handle(MyMsg message)
        {
            await Task.FromResult(false);

            if (Data.Child == null)
            {
                Data.Child = new Child { Property = message.CorrId };
            }

            _messagesBySagaId.AddOrUpdate(Data.Id, 1, (_, count) => count + 1);

            _counter.Decrement();
        }
    }

    class MySagaData : SagaData
    {
        public Child Child { get; set; }
    }

    class Child
    {
        public string Property { get; set; }
    }

    class MyMsg
    {
        public MyMsg(string corrId)
        {
            CorrId = corrId;
        }

        public string CorrId { get;  }
    }
}