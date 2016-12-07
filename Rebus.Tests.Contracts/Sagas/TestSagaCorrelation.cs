using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Sagas;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Contracts.Sagas
{
    public abstract class TestSagaCorrelation<TFactory> : FixtureBase where TFactory : ISagaStorageFactory, new()
    {
        TFactory _factory;

        public TestSagaCorrelation()
        {
            _factory = new TFactory();
        }

        protected override void TearDown()
        {
            _factory.CleanUp();
        }

        [Fact]
        public async Task YeahItWorks()
        {
            var events = new ConcurrentQueue<Tuple<Guid, string>>();
            var activator = new BuiltinHandlerActivator();

            activator.Register((b, context) => new MySaga(events, b));

            Using(activator);

            Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "bimse"))
                .Sagas(s => s.Register(c => _factory.GetSagaStorage()))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(1);
                    o.SetMaxParallelism(1);
                })
                .Start();

            await activator.Bus.SendLocal(new Initiate
            {
                AGuid = new Guid("BAA06058-B34E-4699-8463-E0CBA73E925C"),
                AString = "hej",
                AnInt = 23
            });

            await events.WaitUntil(e => e.Count >= 4);

            await Task.Delay(500);

            Console.WriteLine("GOT EVENTS--------------------------------------------------------");
            Console.WriteLine(string.Join(Environment.NewLine, events));

            Assert.Equal(4, events.Count);

            Assert.Equal(new[]{"initiated!", "int!", "string!", "guid!"}, events.Select(e => e.Item2).ToArray());

            var sagaId = events.First().Item1;

            Assert.True(events.Select(e => e.Item1).All(a => a == sagaId),
                $"Not the same saga ID thoughout: {string.Join(", ", events.Select(e => e.Item1))}");
        }

        public class MySaga : Saga<MySagaData>,
            IAmInitiatedBy<Initiate>,
            IHandleMessages<IntMessage>,
            IHandleMessages<StringMessage>,
            IHandleMessages<GuidMessage>
        {
            readonly ConcurrentQueue<Tuple<Guid, string>> _events;
            readonly IBus _bus;

            public MySaga(ConcurrentQueue<Tuple<Guid, string>> events, IBus bus)
            {
                _events = events;
                _bus = bus;
            }

            protected override void CorrelateMessages(ICorrelationConfig<MySagaData> config)
            {
                // this is silly!
                config.Correlate<Initiate>(m => Guid.NewGuid(), d => d.Id);

                config.Correlate<IntMessage>(m => m.AnInt, d => d.IntValue);
                config.Correlate<GuidMessage>(m => m.AGuid, d => d.GuidValue);
                config.Correlate<StringMessage>(m => m.AString, d => d.StringValue);
            }

            public async Task Handle(Initiate message)
            {
                Data.IntValue = message.AnInt;
                Data.GuidValue = message.AGuid;
                Data.StringValue = message.AString;

                _events.Enqueue(Tuple.Create(Data.Id, "initiated!"));

                await Task.WhenAll(
                    _bus.SendLocal(new IntMessage { AnInt = Data.IntValue }),
                    _bus.SendLocal(new StringMessage { AString = Data.StringValue }),
                    _bus.SendLocal(new GuidMessage { AGuid = Data.GuidValue }));
            }

            public async Task Handle(IntMessage message)
            {
                _events.Enqueue(Tuple.Create(Data.Id, "int!"));
            }

            public async Task Handle(StringMessage message)
            {
                _events.Enqueue(Tuple.Create(Data.Id, "string!"));
            }

            public async Task Handle(GuidMessage message)
            {
                _events.Enqueue(Tuple.Create(Data.Id, "guid!"));
            }
        }

        public class MySagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }

            public Guid GuidValue { get; set; }
            public int IntValue { get; set; }
            public string StringValue { get; set; }
        }

        public class Initiate
        {
            public int AnInt { get; set; }
            public Guid AGuid { get; set; }
            public string AString { get; set; }
        }

        public class IntMessage
        {
            public int AnInt { get; set; }
        }

        public class GuidMessage
        {
            public Guid AGuid { get; set; }
        }

        public class StringMessage
        {
            public string AString { get; set; }
        }
    }

}