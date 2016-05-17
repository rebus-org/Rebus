using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Raven.Client;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.RavenDb.Sagas;
using Rebus.Sagas;
using Rebus.Tests;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;

namespace Rebus.RavenDb.Tests.Sagas.BiggerTest
{
    [TestFixture]
    public class TestRavenDbSagaStorageMoreThoroughly : FixtureBase
    {
        BuiltinHandlerActivator _activator;
        RavenDbSagaStorageFactory _factory;

        IDocumentStore _documentStore;

        protected override void SetUp()
        {
            _factory = new RavenDbSagaStorageFactory();
            _documentStore = _factory.DocumentStore;

            _activator = new BuiltinHandlerActivator();
            Using(_activator);

            Configure.With(_activator)
                .Logging(l => l.Console(minLevel: LogLevel.Error))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "raven"))
                .Sagas(s => s.StoreInRavenDb(_documentStore))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(1);
                    o.SetMaxParallelism(2);
                })
                .Start();
        }

        protected override void TearDown()
        {
            _factory.CleanUp();
        }

        [Test]
        public async Task EndsUpInConsistentState()
        {
            _activator.Register(() => new RavenSagaHandler());

            var messages = Enumerable.Repeat(new Message1("saga1"), 10).Cast<object>()
                .Concat(Enumerable.Repeat(new Message2("saga1"), 10))
                .Concat(Enumerable.Repeat(new Message1("saga2"), 10))
                .Concat(Enumerable.Repeat(new Message2("saga2"), 10))
                .InRandomOrder();

            await Task.WhenAll(messages.Select(m => _activator.Bus.SendLocal(m)));

            await Task.Delay(10000);

            using (var session = _documentStore.OpenSession())
            {
                var allDocuments = session.Query<RavenDbSagaStorage.SagaDataDocument>()
                    .Customize(c => c.WaitForNonStaleResults())
                    .ToList()
                    .Select(d => d.SagaData)
                    .Cast<RavenSagaData>()
                    .ToList();

                Console.WriteLine(JsonConvert.SerializeObject(allDocuments, Formatting.Indented));

                Assert.That(allDocuments.Count, Is.EqualTo(2));

                var saga1 = allDocuments.First(s => s.CorrelationId1 == "saga1");
                var saga2 = allDocuments.First(s => s.CorrelationId1 == "saga2");

                Assert.That(saga1.Count1, Is.EqualTo(10));
                Assert.That(saga1.Count2, Is.EqualTo(10));
                Assert.That(saga2.Count1, Is.EqualTo(10));
                Assert.That(saga2.Count2, Is.EqualTo(10));
            }
        }
    }

    class Message1
    {
        public Message1(string correlationId)
        {
            CorrelationId = correlationId;
        }

        public string CorrelationId { get; }

        public override string ToString()
        {
            return $"Message1 => {CorrelationId}";
        }
    }

    class Message2
    {
        public Message2(string correlationId)
        {
            CorrelationId = correlationId;
        }

        public string CorrelationId { get; }

        public override string ToString()
        {
            return $"Message2 => {CorrelationId}";
        }
    }

    class RavenSagaHandler : Saga<RavenSagaData>, IAmInitiatedBy<Message1>, IAmInitiatedBy<Message2>
    {
        protected override void CorrelateMessages(ICorrelationConfig<RavenSagaData> config)
        {
            config.Correlate<Message1>(m => m.CorrelationId, d => d.CorrelationId1);
            config.Correlate<Message2>(m => m.CorrelationId, d => d.CorrelationId2);
        }

        public async Task Handle(Message1 message)
        {
            if (IsNew)
            {
                Data.CorrelationId1 = message.CorrelationId;
                Data.CorrelationId2 = message.CorrelationId;
            }

            Data.Count1++;
        }

        public async Task Handle(Message2 message)
        {
            if (IsNew)
            {
                Data.CorrelationId1 = message.CorrelationId;
                Data.CorrelationId2 = message.CorrelationId;
            }

            Data.Count2++;
        }
    }

    class RavenSagaData : ISagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }

        public int Count1 { get; set; }
        public int Count2 { get; set; }

        public string CorrelationId1 { get; set; }
        public string CorrelationId2 { get; set; }
    }
}