using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Sagas;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Integration
{
    public class TestSagaIsNew : FixtureBase
    {
        readonly BuiltinHandlerActivator _activator;
        readonly IBus _bus;

        public TestSagaIsNew()
        {
            _activator = Using(new BuiltinHandlerActivator());

            _bus = Configure.With(_activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "saga_is_new"))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(1);
                    o.SetMaxParallelism(1);
                })
                .Start();
        }

        [Fact]
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

            await Task.WhenAll(messages.Select(message => _bus.SendLocal(message)));

            counter.WaitForResetEvent();

            Assert.Equal(4, eventsPerCorrelationId.Count);

            Assert.Equal(new[] { true, false, false },eventsPerCorrelationId["1"]);
            Assert.Equal(new[] { true, false },eventsPerCorrelationId["2"]);
            Assert.Equal(new[] { true, },eventsPerCorrelationId["3"]);
            Assert.Equal(new[] { true, false, false, false, false, false, },eventsPerCorrelationId["4"]);
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
}