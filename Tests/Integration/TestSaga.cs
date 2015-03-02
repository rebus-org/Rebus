using System;
using System.Collections.Generic;
using System.Configuration.Internal;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus2.Activation;
using Rebus2.Bus;
using Rebus2.Config;
using Rebus2.Handlers;
using Rebus2.Logging;
using Rebus2.Messages;
using Rebus2.Sagas;
using Rebus2.Transport.InMem;

namespace Tests.Integration
{
    [TestFixture]
    public class TestSaga : FixtureBase
    {
        BuiltinHandlerActivator _handlerActivator;
        IBus _bus;

        protected override void SetUp()
        {
            _handlerActivator = new BuiltinHandlerActivator();

            _handlerActivator.Register(() => new MySaga());

            _bus = Configure.With(_handlerActivator)
                .Logging(l => l.Console(minLevel: LogLevel.Debug))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(true), "test.sagas.input"))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(1);
                    o.SetMaxParallelism(1);
                })
                .Start();

            TrackDisposable(_bus);
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
            protected override void CorrelateMessages(ICorrelationConfig<MySagaData> config)
            {
                config.Correlate<InitiatingMessage>(m => m.CorrelationId, d => d.CorrelationId);
                config.Correlate<CorrelatedMessage>(m => m.CorrelationId, d => d.CorrelationId);
            }

            public async Task Handle(InitiatingMessage message)
            {
                Data.CorrelationId = message.CorrelationId;

                Increment(message.GetType());
            }

            public async Task Handle(CorrelatedMessage message)
            {
                Increment(message.GetType());
            }

            void Increment(Type type)
            {
                if (!Data.ProcessedMessages.ContainsKey(type))
                    Data.ProcessedMessages[type] = 0;

                Data.ProcessedMessages[type]++;
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

            public Dictionary<Type, int> ProcessedMessages { get; set; }
        }
    }

}