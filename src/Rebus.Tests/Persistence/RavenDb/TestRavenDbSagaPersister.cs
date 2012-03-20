using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Raven.Client.Embedded;
using Rebus.Persistence.InMemory;
using Rebus.RavenDb;
using Shouldly;

namespace Rebus.Tests.Persistence.RavenDb
{
    [TestFixture]
    public class TestRavenDbSagaPersister
    {
        RavenDbSagaPersister persister;
        EmbeddableDocumentStore store;

        [SetUp]
        public void SetUp()
        {
            store = new EmbeddableDocumentStore
            {
                RunInMemory = true
            };
            store.Initialize();

            persister = new RavenDbSagaPersister(store, "Sagas");
        }

        [Theory]
        public void PersisterCanFindSagaByPropertiesWithDifferentDataTypes([Values("Hello world!!!", 23)] object data)
        {
            TestWithType(data);
        }

        void TestWithType<TProperty>(TProperty propertyValueToUse)
        {
            var propertyTypeToTest = typeof (TProperty);

            var sagaDataType = typeof (GenericSagaData<>).MakeGenericType(propertyTypeToTest);
            var savedSagaData = (ISagaData) Activator.CreateInstance(sagaDataType);
            var savedSagaDataId = Guid.NewGuid();
            savedSagaData.Id = savedSagaDataId;
            sagaDataType.GetProperty("Property").SetValue(savedSagaData, propertyValueToUse, new object[0]);
            persister.Save(savedSagaData, new[] { "Property" });

            var foundSagaData = persister.Find("Property", propertyValueToUse, sagaDataType);

            foundSagaData.ShouldNotBe(null);
            foundSagaData.Id.ShouldBe(savedSagaDataId);
        }

        [Test]
        public void UsesOptimisticLockingAndDetectsRaceConditionsWhenUpdating()
        {
            var indexBySomeString = new[] { "SomeString" };
            var id = Guid.NewGuid();
            var simpleSagaData = new SimpleSagaData { Id = id, SomeString = "hello world!" };
            persister.Save(simpleSagaData, indexBySomeString);

            var sagaData1 = (SimpleSagaData) persister.Find("SomeString", "hello world!", typeof (SimpleSagaData));
            var sagaData2 = (SimpleSagaData) persister.Find("SomeString", "hello world!", typeof (SimpleSagaData));

            persister.Save(sagaData1, indexBySomeString);
            var exception = Assert.Throws<OptimisticLockingException>(() => persister.Save(sagaData2, indexBySomeString));
            Console.WriteLine(exception);
        }

        [Test]
        public void PersistsComplexSagaLikeExpected()
        {
            var sagaDataId = Guid.NewGuid();

            var complexPieceOfSagaData =
                new MySagaData
                {
                    Id = sagaDataId,
                    SomeField = "hello",
                    AnotherField = "world!",
                    Embedded = new SomeEmbeddedThingie
                    {
                        ThisIsEmbedded = "this is embedded",
                        Thingies =
                            {
                                new SomeCollectedThing { No = 1 },
                                new SomeCollectedThing { No = 2 },
                                new SomeCollectedThing { No = 3 },
                                new SomeCollectedThing { No = 4 },
                            }
                    }
                };

            persister.Save(complexPieceOfSagaData, new[] { "SomeField" });

            using (var session = store.OpenSession())
            {
                var sagaData = session.Load<MySagaData>("Sagas/" + sagaDataId);
                sagaData.ShouldNotBe(null);
                sagaData.SomeField.ShouldBe("hello");
                sagaData.AnotherField.ShouldBe("world!");
            }
        }

        [Test]
        public void CanDeleteSaga()
        {
            var mySagaDataId = Guid.NewGuid();
            var mySagaData = new SimpleSagaData
            {
                Id = mySagaDataId,
                SomeString = "whoolala"
            };

            persister.Save(mySagaData, new[] { "SomeString" });

            persister.Delete(mySagaData);

            using (var session = store.OpenSession())
            {
                var loadedSagaData = session.Load<SimpleSagaData>("Sagas/" + mySagaDataId);
                loadedSagaData.ShouldBe(null);
            }
        }

        [Test]
        public void CanFindSagaByPropertyValues()
        {
            persister.Save(SagaData(1, "some field 1"), new[] { "AnotherField" });
            persister.Save(SagaData(2, "some field 2"), new[] { "AnotherField" });
            persister.Save(SagaData(3, "some field 3"), new[] { "AnotherField" });

            var sagaDataType = typeof (MySagaData);
            var dataViaNonexistentValue = persister.Find("AnotherField", "non-existent value", sagaDataType);
            var dataViaNonexistentField = persister.Find("SomeFieldThatDoesNotExist", "doesn't matter", sagaDataType);
            var mySagaData = ((MySagaData) persister.Find("AnotherField", "some field 2", sagaDataType));

            dataViaNonexistentField.ShouldBe(null);
            dataViaNonexistentValue.ShouldBe(null);
            mySagaData.ShouldNotBe(null);
            mySagaData.SomeField.ShouldBe("2");
        }


        MySagaData SagaData(int someNumber, string textInSomeField)
        {
            return new MySagaData
            {
                Id = Guid.NewGuid(),
                SomeField = someNumber.ToString(),
                AnotherField = textInSomeField,
            };
        }

        class GenericSagaData<T> : ISagaData
        {
            public T Property { get; set; }
            public Guid Id { get; set; }
            public int Revision { get; set; }
        }

        class MySagaData : ISagaData
        {
            public string SomeField { get; set; }
            public string AnotherField { get; set; }
            public SomeEmbeddedThingie Embedded { get; set; }
            public Guid Id { get; set; }

            public int Revision { get; set; }
        }

        class SimpleSagaData : ISagaData
        {
            public string SomeString { get; set; }
            public Guid Id { get; set; }
            public int Revision { get; set; }
        }

        class SomeCollectedThing
        {
            public int No { get; set; }
        }

        class SomeEmbeddedThingie
        {
            public SomeEmbeddedThingie()
            {
                Thingies = new List<SomeCollectedThing>();
            }

            public string ThisIsEmbedded { get; set; }
            public List<SomeCollectedThing> Thingies { get; set; }
        }
    }

    public class SagasNotWorkingWithRavenDbEnlistingInTransaction : RebusBusMsmqIntegrationTestBase
    {
        const string Queue = "test.publisher";

        [Test]
        public void ShouldWork()
        {
            var store = new EmbeddableDocumentStore
                        {
                            RunInMemory = true
                        };

            store.Initialize();

            var activator = new HandlerActivatorForTesting();
            var checker = new CheckCallsMade();
            var bus = CreateBus(Queue, activator, new InMemorySubscriptionStorage(), new RavenDbSagaPersister(store, "Sagas")).Start(1);
            activator.UseHandler(() => new TheSaga(bus, checker));
            bus.Send(new TheFirstMessage());

            Thread.Sleep(15000);
            Assert.IsTrue(checker.First, "First should be called");
            Assert.IsTrue(checker.Second, "Second should be called");
        }

        public override string GetEndpointFor(Type messageType)
        {
            return Queue;
        }

        public class TheFirstMessage
        {
        }

        public class TheSecondMessage
        {
            public Guid CorrelationId { get; set; }
        }

        public class TheSaga : Saga<TheSaga.SomeSagaData>,
            IAmInitiatedBy<TheFirstMessage>,
            IHandleMessages<TheSecondMessage>
        {
            private readonly IBus bus;
            private readonly CheckCallsMade checker;

            public TheSaga(IBus bus, CheckCallsMade checker)
            {
                this.bus = bus;
                this.checker = checker;
            }

            public override void ConfigureHowToFindSaga()
            {
                Incoming<TheSecondMessage>(x => x.CorrelationId).CorrelatesWith(x => x.Id);
            }

            public class SomeSagaData : ISagaData
            {
                public SomeSagaData()
                {
                    Id = Guid.NewGuid();
                }

                public Guid Id { get; set; }
                public int Revision { get; set; }
            }

            public void Handle(TheFirstMessage message)
            {
                checker.First = true;
                bus.SendLocal(new TheSecondMessage
                {
                    CorrelationId = Data.Id
                });
            }

            public void Handle(TheSecondMessage message)
            {
                checker.Second = true;
            }
        }
    }

    public class CheckCallsMade
    {
        public bool First { get; set; }
        public bool Second { get; set; }
    }
}