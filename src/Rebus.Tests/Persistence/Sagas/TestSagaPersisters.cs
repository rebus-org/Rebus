using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Persistence.InMemory;
using Rhino.Mocks;
using Shouldly;

namespace Rebus.Tests.Persistence.Sagas
{
    [TestFixture(typeof(InMemorySagaPersisterFactory))]
    [TestFixture(typeof(SqlServerSagaPersisterFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(RavenDbSagaPersisterFactory), Category = TestCategories.Raven)]
    [TestFixture(typeof(MongoDbSagaPersisterFactory), Category = TestCategories.Mongo)]
    public class TestSagaPersisters<TFactory> : FixtureBase where TFactory : ISagaPersisterFactory
    {
        MessageContext messageContext;
        TFactory factory;
        IStoreSagaData persister;

        protected override void DoSetUp()
        {
            factory = Activator.CreateInstance<TFactory>();
            persister = factory.CreatePersister();
            messageContext = MessageContext.Enter("none");
        }

        protected override void DoTearDown()
        {
            factory.Dispose();
            messageContext.Dispose();
        }

        [Test]
        public void PersisterCanFindSagaByPropertiesWithDifferentDataTypes()
        {
            TestFindSagaByPropertyWithType("Hello worlds!!");
            TestFindSagaByPropertyWithType(23);
            TestFindSagaByPropertyWithType(Guid.NewGuid());
        }

        void TestFindSagaByPropertyWithType<TProperty>(TProperty propertyValueToUse)
        {
            var propertyTypeToTest = typeof(TProperty);
            var type = typeof (GenericSagaData<>);
            var sagaDataType = type.MakeGenericType(typeof(TFactory), propertyTypeToTest);
            var savedSagaData = (ISagaData)Activator.CreateInstance(sagaDataType);
            var savedSagaDataId = Guid.NewGuid();
            savedSagaData.Id = savedSagaDataId;
            sagaDataType.GetProperty("Property").SetValue(savedSagaData, propertyValueToUse, new object[0]);
            persister.Save(savedSagaData, new[] { "Property" });

            var foundSagaData = persister.Find<GenericSagaData<TProperty>>("Property", propertyValueToUse);

            foundSagaData.ShouldNotBe(null);
            foundSagaData.Id.ShouldBe(savedSagaDataId);
        }

        [Test]
        public void PersisterCanFindSagaById()
        {
            var savedSagaData = new MySagaData();
            var savedSagaDataId = Guid.NewGuid();
            savedSagaData.Id = savedSagaDataId;
            persister.Save(savedSagaData, new string[0]);

            var foundSagaData = persister.Find<MySagaData>("Id", savedSagaDataId);

            foundSagaData.ShouldNotBe(null);
            foundSagaData.Id.ShouldBe(savedSagaDataId);
        }

        [Test]
        public void UsesOptimisticLockingAndDetectsRaceConditionsWhenUpdatingFindingBySomeProperty()
        {
            if (persister is InMemorySagaPersister)
                Assert.Ignore("Not applicable for InMemorySagaPersister");

            var indexBySomeString = new[] { "SomeString" };
            var id = Guid.NewGuid();
            var simpleSagaData = new SimpleSagaData { Id = id, SomeString = "hello world!" };
            persister.Save(simpleSagaData, indexBySomeString);

            var sagaData1 = persister.Find<SimpleSagaData>("SomeString", "hello world!");
            sagaData1.SomeString = "I changed this on one worker";

            EnterAFakeMessageContext();

            var sagaData2 = persister.Find<SimpleSagaData>("SomeString", "hello world!");
            sagaData2.SomeString = "I changed this on another worker";
            persister.Save(sagaData2, indexBySomeString);

            ReturnToOriginalMessageContext();

            Assert.Throws<OptimisticLockingException>(() => persister.Save(sagaData1, indexBySomeString));
        }

        [Test]
        public void UsesOptimisticLockingAndDetectsRaceConditionsWhenUpdatingFindingById()
        {
            if(persister is InMemorySagaPersister)
                Assert.Ignore("Not applicable for InMemorySagaPersister");

            var indexBySomeString = new[] { "Id" };
            var id = Guid.NewGuid();
            var simpleSagaData = new SimpleSagaData { Id = id, SomeString = "hello world!" };
            persister.Save(simpleSagaData, indexBySomeString);

            var sagaData1 = persister.Find<SimpleSagaData>("Id", id);
            sagaData1.SomeString = "I changed this on one worker";

            EnterAFakeMessageContext();

            var sagaData2 = persister.Find<SimpleSagaData>("Id", id);
            sagaData2.SomeString = "I changed this on another worker";
            persister.Save(sagaData2, indexBySomeString);

            ReturnToOriginalMessageContext();

            Assert.Throws<OptimisticLockingException>(() => persister.Save(sagaData1, indexBySomeString));
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
                                new SomeCollectedThing { No = 4 }
                            }
                    }
                };

            persister.Save(complexPieceOfSagaData, new[] { "SomeField" });

            var sagaData = persister.Find<MySagaData>("Id", sagaDataId);
            sagaData.ShouldNotBe(null);
            sagaData.SomeField.ShouldBe("hello");
            sagaData.AnotherField.ShouldBe("world!");
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

            var sagaData = persister.Find<SimpleSagaData>("Id", mySagaDataId);
            sagaData.ShouldBe(null);
        }

        [Test]
        public void CanFindSagaByPropertyValues()
        {
            persister.Save(SagaData(1, "some field 1"), new[] { "AnotherField" });
            persister.Save(SagaData(2, "some field 2"), new[] { "AnotherField" });
            persister.Save(SagaData(3, "some field 3"), new[] { "AnotherField" });

            var dataViaNonexistentValue = persister.Find<MySagaData>("AnotherField", "non-existent value");
            var dataViaNonexistentField = persister.Find<MySagaData>("SomeFieldThatDoesNotExist", "doesn't matter");
            var mySagaData = persister.Find<MySagaData>("AnotherField", "some field 2");

            dataViaNonexistentField.ShouldBe(null);
            dataViaNonexistentValue.ShouldBe(null);
            mySagaData.ShouldNotBe(null);
            mySagaData.SomeField.ShouldBe("2");
        }

        void ReturnToOriginalMessageContext()
        {
            FakeMessageContext.Establish(messageContext);
        }

        void EnterAFakeMessageContext()
        {
            var fakeConcurrentMessageContext = Mock<IMessageContext>();
            var otherItems = new Dictionary<string, object>();
            fakeConcurrentMessageContext.Stub(x => x.Items).Return(otherItems);
            FakeMessageContext.Establish(fakeConcurrentMessageContext);
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
}