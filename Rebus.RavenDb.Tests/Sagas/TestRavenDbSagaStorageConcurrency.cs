using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Raven.Imports.Newtonsoft.Json;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.RavenDb.Sagas;
using Rebus.RavenDb.Tests.Sagas.Models;
using Rebus.Sagas;
using Rebus.Tests;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;

namespace Rebus.RavenDb.Tests.Sagas
{
    [TestFixture]
    public class TestRavenDbSagaStorageConcurrency : FixtureBase
    {
        BuiltinHandlerActivator _activator;
        RavenDbSagaStorageFactory _factory;

        protected override void SetUp()
        {
            _factory = new RavenDbSagaStorageFactory();

            _activator = Using(new BuiltinHandlerActivator());

            Configure.With(_activator)
                .Logging(l => l.Console(LogLevel.Info))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "stresstest"))
                .Sagas(s => s.StoreInRavenDb(_factory.DocumentStore))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(0);
                    
                    // pretty parallel!!
                    o.SetMaxParallelism(100);
                })
                .Start();
        }

        protected override void TearDown()
        {
            _factory.CleanUp();
        }

        [Test]
        public void CountsAsExpected()
        {
            _activator.Register(() => new CountingSaga());

            Console.WriteLine("Queueing up some messages");

            10.Times(() => _activator.Bus.SendLocal("hej!").Wait());

            Console.WriteLine("Starting adding 10 workers");

            _activator.Bus.Advanced.Workers.SetNumberOfWorkers(10);

            Console.WriteLine("Waiting a while...");

            Thread.Sleep(5000);

            using (var session = _factory.DocumentStore.OpenSession())
            {
                var allDocuments = session.Query<RavenDbSagaStorage.SagaDataDocument>()
                    .Customize(c => c.WaitForNonStaleResults())
                    .ToList();

                Console.WriteLine(JsonConvert.SerializeObject(allDocuments));

                Assert.That(allDocuments.Count, Is.EqualTo(1));
            }
        }

        class CountingSaga : Saga<BasicSagaData>, IAmInitiatedBy<string>
        {
            protected override void CorrelateMessages(ICorrelationConfig<BasicSagaData> config)
            {
                config.Correlate<string>(msg => msg, d => d.StringField);
            }

            public async Task Handle(string message)
            {
                Data.StringField = message;
                Data.IntegerField ++;
            }
        }
    }
}