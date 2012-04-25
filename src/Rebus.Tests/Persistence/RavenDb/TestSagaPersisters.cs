using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Raven.Client.Embedded;
using Rebus.RavenDb;
using Rhino.Mocks;
using Shouldly;

namespace Rebus.Tests.Persistence.RavenDb
{
    public class RavenDbSagaPersisterFactory : IPersisterFactory
    {
        EmbeddableDocumentStore store;

        public IStoreSagaData CreatePersister()
        {
            store = new EmbeddableDocumentStore
            {
                RunInMemory = true
            };
            store.Initialize();

            return new RavenDbSagaPersister(store);            
        }

        public void Dispose()
        {
            store.Dispose();
        }
    }

    [TestFixture(typeof(RavenDbSagaPersisterFactory))]
    public class TestSagaPersisters<TFactory> : FixtureBase where TFactory : IPersisterFactory
    {
        MessageContext messageContext;
        TFactory factory;
        IStoreSagaData persister;

        public TestSagaPersisters()
        {
            
        }

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

        //[Test]
        //public void PersisterCanFindSagaByPropertiesWithDifferentDataTypes()
        //{
        //    TestWithType("Hello worlds!!");
        //    TestWithType(23);
        //}

        //void TestWithType<TProperty>(TProperty propertyValueToUse)
        //{
        //    var propertyTypeToTest = typeof (TProperty);

        //    var sagaDataType = typeof (GenericSagaData<>).MakeGenericType(propertyTypeToTest);
        //    var savedSagaData = (ISagaData) Activator.CreateInstance(sagaDataType);
        //    var savedSagaDataId = Guid.NewGuid();
        //    savedSagaData.Id = savedSagaDataId;
        //    sagaDataType.GetProperty("Property").SetValue(savedSagaData, propertyValueToUse, new object[0]);
        //    persister.Save(savedSagaData, new[] { "Property" });

        //    var foundSagaData = persister.Find<GenericSagaData<TProperty>>("Property", propertyValueToUse);

        //    foundSagaData.ShouldNotBe(null);
        //    foundSagaData.Id.ShouldBe(savedSagaDataId);
        //}

        [Test]
        public void PersisterCanFindSagaById()
        {
            var savedSagaData = new MySagaData();
            var savedSagaDataId = Guid.NewGuid();
            savedSagaData.Id = savedSagaDataId;
            persister.Save(savedSagaData, null);

            var foundSagaData = persister.Find<MySagaData>("Id", savedSagaDataId);

            foundSagaData.ShouldNotBe(null);
            foundSagaData.Id.ShouldBe(savedSagaDataId);
        }

        //[Test]
        //public void UsesOptimisticLockingAndDetectsRaceConditionsWhenUpdating()
        //{
        //    var indexBySomeString = new[] { "SomeString" };
        //    var id = Guid.NewGuid();
        //    var simpleSagaData = new SimpleSagaData { Id = id, SomeString = "hello world!" };
        //    persister.Save(simpleSagaData, indexBySomeString);

        //    var sagaData1 = persister.Find<SimpleSagaData>("SomeString", "hello world!");
        //    sagaData1.Revision++;

        //    EnterAFakeMessageContext();
            
        //    var sagaData2 = persister.Find<SimpleSagaData>("SomeString", "hello world!");
        //    sagaData2.Revision++;
        //    persister.Save(sagaData2, indexBySomeString);

        //    ReturnToOriginalMessageContext();

        //    var exception = Assert.Throws<OptimisticLockingException>(() => persister.Save(sagaData1, indexBySomeString));
        //    Console.WriteLine(exception);
        //}

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

        //[Test]
        //public void UsesOptimisticLockingAndDetectsRaceConditionsWhenUpdating2()
        //{
        //    var indexBySomeString = new[] { "Id" };
        //    var id = Guid.NewGuid();
        //    var simpleSagaData = new SimpleSagaData { Id = id, SomeString = "hello world!" };
        //    persister.Save(simpleSagaData, indexBySomeString);

        //    var sagaData1 = persister.Find<SimpleSagaData>("Id", id);
        //    sagaData1.Revision++;

        //    EnterAFakeMessageContext();
        //    var sagaData2 = persister.Find<SimpleSagaData>("Id", id);
        //    sagaData2.Revision++;
        //    persister.Save(sagaData2, indexBySomeString);
        //    ReturnToOriginalMessageContext();

        //    var exception = Assert.Throws<OptimisticLockingException>(() => persister.Save(sagaData1, indexBySomeString));
        //    Console.WriteLine(exception);
        //}

        //[Test]
        //public void PersistsComplexSagaLikeExpected()
        //{
        //    var sagaDataId = Guid.NewGuid();

        //    var complexPieceOfSagaData =
        //        new MySagaData
        //        {
        //            Id = sagaDataId,
        //            SomeField = "hello",
        //            AnotherField = "world!",
        //            Embedded = new SomeEmbeddedThingie
        //            {
        //                ThisIsEmbedded = "this is embedded",
        //                Thingies =
        //                    {
        //                        new SomeCollectedThing { No = 1 },
        //                        new SomeCollectedThing { No = 2 },
        //                        new SomeCollectedThing { No = 3 },
        //                        new SomeCollectedThing { No = 4 },
        //                    }
        //            }
        //        };

        //    persister.Save(complexPieceOfSagaData, new[] { "SomeField" });

        //    using (var session = store.OpenSession())
        //    {
        //        var sagaData = session.Load<MySagaData>(sagaDataId);
        //        sagaData.ShouldNotBe(null);
        //        sagaData.SomeField.ShouldBe("hello");
        //        sagaData.AnotherField.ShouldBe("world!");
        //    }
        //}

        //[Test]
        //public void CanDeleteSaga()
        //{
        //    var mySagaDataId = Guid.NewGuid();
        //    var mySagaData = new SimpleSagaData
        //    {
        //        Id = mySagaDataId,
        //        SomeString = "whoolala"
        //    };

        //    persister.Save(mySagaData, new[] { "SomeString" });

        //    persister.Delete(mySagaData);

        //    using (var session = store.OpenSession())
        //    {
        //        var loadedSagaData = session.Load<SimpleSagaData>(mySagaDataId);
        //        loadedSagaData.ShouldBe(null);
        //    }
        //}

        //[Test]
        //public void CanFindSagaByPropertyValues()
        //{
        //    persister.Save(SagaData(1, "some field 1"), new[] { "AnotherField" });
        //    persister.Save(SagaData(2, "some field 2"), new[] { "AnotherField" });
        //    persister.Save(SagaData(3, "some field 3"), new[] { "AnotherField" });

        //    var dataViaNonexistentValue = persister.Find<MySagaData>("AnotherField", "non-existent value");
        //    var dataViaNonexistentField = persister.Find<MySagaData>("SomeFieldThatDoesNotExist", "doesn't matter");
        //    var mySagaData = persister.Find<MySagaData>("AnotherField", "some field 2");

        //    dataViaNonexistentField.ShouldBe(null);
        //    dataViaNonexistentValue.ShouldBe(null);
        //    mySagaData.ShouldNotBe(null);
        //    mySagaData.SomeField.ShouldBe("2");
        //}


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

    public interface IPersisterFactory : IDisposable
    {
        IStoreSagaData CreatePersister();
    }
}