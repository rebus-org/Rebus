using System;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.MongoDb;
using Rebus.Persistence.InMemory;
using Rebus.Serialization.Json;
using Rebus.Tests.Persistence;
using Rebus.Transports.Msmq;
using Shouldly;
using System.Linq;

namespace Rebus.Tests.Integration
{
    [TestFixture, Category(TestCategories.Integration), Category(TestCategories.Mongo)]
    public class TestBusAndDispatcherWithMongoSaga : MongoDbFixtureBase
    {
        RebusBus bus;
        HandlerActivatorForTesting handlers;

        protected override void DoSetUp()
        {
            DropCollection("sagas");

            var msmqMessageQueue = new MsmqMessageQueue("test.dispatcher.and.mongo");
            handlers = new HandlerActivatorForTesting().UseHandler(new MySaga());
            
            var persister = new MongoDbSagaPersister(ConnectionString)
                .SetCollectionName<MySagaData>("sagas");
            
            bus = new RebusBus(handlers, msmqMessageQueue,
                               msmqMessageQueue, new InMemorySubscriptionStorage(),
                               persister,
                               null,
                               new JsonMessageSerializer(),
                               new TrivialPipelineInspector(),
                               new ErrorTracker("error"),
                               null,
                               new ConfigureAdditionalBehavior())
                .Start(1);
        }

        protected override void DoTearDown()
        {
            bus.Dispose();
        }

        [Test]
        public void CanDispatchToSagaAndCorrelateWithNestedGuid()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            bus.SendLocal(new HasGuid{Guid = id1});
            bus.SendLocal(new HasGuid{Guid = id1});
            bus.SendLocal(new HasGuid{Guid = id2});
            bus.SendLocal(new HasGuid{Guid = id2});
            bus.SendLocal(new HasGuid{Guid = id2});

            Thread.Sleep(5.Seconds());

            var sagas = Collection<MySagaData>("sagas");
            var allSagas = sagas.FindAll();

            allSagas.Count().ShouldBe(2);
            var saga1 = allSagas.Single(s => s.NestedData.CorrelationId == id1);
            var saga2 = allSagas.Single(s => s.NestedData.CorrelationId == id2);
            saga1.NestedData.Counter.ShouldBe(2);
            saga2.NestedData.Counter.ShouldBe(3);
        }

        class MySaga : Saga<MySagaData>, IAmInitiatedBy<HasGuid>
        {
            public override void ConfigureHowToFindSaga()
            {
                Incoming<HasGuid>(m => m.Guid).CorrelatesWith(d => d.NestedData.CorrelationId);
            }

            public void Handle(HasGuid message)
            {
                if (Data.NestedData == null) 
                    Data.NestedData = new NestedData();

                Data.NestedData.CorrelationId = message.Guid;
                Data.NestedData.Counter++;
            }
        }

        class MySagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }

            public NestedData NestedData { get; set; }
        }

        class NestedData
        {
            public Guid CorrelationId { get; set; }
            public int Counter { get; set; }
        }

        class HasGuid
        {
            public Guid Guid { get; set; }

            public override string ToString()
            {
                return string.Format("Msg with {0}", Guid);
            }
        }
    }

}