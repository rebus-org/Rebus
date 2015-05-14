using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Sagas;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Contracts.Sagas
{
    public abstract class SagaIntegrationTests<TFactory> : FixtureBase where TFactory : ISagaStorageFactory, new()
    {
        TFactory _factory;

        protected override void SetUp()
        {
            _factory = new TFactory();
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

            await bus.SendLocal(new SagaMessage {Id = 70});

            const int millisecondsDelay = 300;

            await Task.Delay(millisecondsDelay);
            await bus.SendLocal(new SagaMessage { Id = 70 });
            
            await Task.Delay(millisecondsDelay);
            await bus.SendLocal(new SagaMessage { Id = 70 });
            
            await Task.Delay(millisecondsDelay);
            await bus.SendLocal(new SagaMessage { Id = 70 });
            
            await Task.Delay(millisecondsDelay);
            await bus.SendLocal(new SagaMessage { Id = 70 });

            await Task.Delay(1000);

            Assert.That(events.ToArray(), Is.EqualTo(new[]
            {
                "70:1",
                "70:2",
                "70:3", // it is marked as completed here
                "70:1",
                "70:2",
            }));
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

                _stuff.Enqueue(string.Format("{0}:{1}", Data.CorrelationId, Data.NumberOfProcessedMessages));

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

}