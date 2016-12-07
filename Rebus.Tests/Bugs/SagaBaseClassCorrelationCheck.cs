using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Sagas;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Bugs
{
    //Tests scenario where the same saga data type is used in two saga handlers that use different correlation properties. This should be possible
    public class SagaBaseClassCorrelationCheck : FixtureBase
    {
        [Fact]
        public async Task CanHitBothSagas()
        {
            var activator = Using(new BuiltinHandlerActivator());
            var events = new ConcurrentQueue<string>();

            activator
                .Register(() => new MySaga1(events))
                .Register(() => new MySaga2(events));

            Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "two sagas with same type of saga data"))
                .Start();

            await activator.Bus.SendLocal(new Message1 { CorrelationId = "correlation1" });

            await activator.Bus.SendLocal(new Message2 { CorrelationId = "correlation2" });

            await events.WaitUntil(q => q.Count == 2);

            Assert.Equal(new[] {"Handling Message1", "Handling Message2" }, events.OrderBy(e => e));
        }

        class Message1
        {
            public string CorrelationId { get; set; }
        }

        class Message2
        {
            public string CorrelationId { get; set; }
        }

        class MySaga1 : Saga<MySagaData>, IAmInitiatedBy<Message1>
        {
            readonly ConcurrentQueue<string> _events;

            public MySaga1(ConcurrentQueue<string> events)
            {
                _events = events;
            }

            protected override void CorrelateMessages(ICorrelationConfig<MySagaData> config)
            {
                config.Correlate<Message1>(m => m.CorrelationId, d => d.CorrelationProperty1);
            }

            public async Task Handle(Message1 message)
            {
                _events.Enqueue($"Handling {message.GetType().Name}");
            }
        }

        class MySaga2 : Saga<MySagaData>, IAmInitiatedBy<Message2>
        {
            readonly ConcurrentQueue<string> _events;

            public MySaga2(ConcurrentQueue<string> events)
            {
                _events = events;
            }

            protected override void CorrelateMessages(ICorrelationConfig<MySagaData> config)
            {
                config.Correlate<Message2>(m => m.CorrelationId, d => d.CorrelationProperty1);
            }

            public async Task Handle(Message2 message)
            {
                _events.Enqueue($"Handling {message.GetType().Name}");
            }
        }

        class MySagaData : SagaData
        {
            public string CorrelationProperty1 { get; set; }
            public string CorrelationProperty2 { get; set; }
        }
    }
}