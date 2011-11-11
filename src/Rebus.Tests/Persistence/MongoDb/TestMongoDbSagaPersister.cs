using System;
using System.Collections.Generic;
using MongoDB.Driver.Builders;
using NUnit.Framework;
using Rebus.MongoDb;
using Shouldly;

namespace Rebus.Tests.Persistence.MongoDb
{
    [TestFixture, Category(TestCategories.Mongo)]
    public class TestMongoDbSagaPersister : MongoDbFixtureBase
    {
        MongoDbSagaPersister persister;

        protected override void DoSetUp()
        {
            persister = new MongoDbSagaPersister(ConnectionString, "sagas");
        }

        protected override void DoTearDown()
        {
            DropCollection("sagas");
        }

        [Test, Ignore("haven't found a solution for this yet!!"), Category(TestCategories.ToDo)]
        public void PersisterCanFindSagaByPropertiesWithDifferentDataTypes()
        {
            DropCollection("sagas");
            TestWithType("Hello world!!!");

            DropCollection("sagas");
            TestWithType(23);
            
            DropCollection("sagas");
            TestWithType(Guid.NewGuid());
        }

        void TestWithType<TProperty>(TProperty propertyValueToUse)
        {
            var propertyTypeToTest = typeof(TProperty);

            try
            {
                // arrange
                var sagaDataType = typeof(GenericSagaData<>).MakeGenericType(propertyTypeToTest);
                var savedSagaData = (ISagaData)Activator.CreateInstance(sagaDataType);
                var savedSagaDataId = Guid.NewGuid();
                savedSagaData.Id = savedSagaDataId;
                sagaDataType.GetProperty("Property").SetValue(savedSagaData, propertyValueToUse, new object[0]);
                persister.Save(savedSagaData, new[] { "Property" });

                // act
                var foundSagaData = persister.Find("Property", propertyValueToUse.ToString(), sagaDataType);

                // assert
                foundSagaData.ShouldNotBe(null);
                foundSagaData.Id.ShouldBe(savedSagaDataId);
            }
            catch (Exception exception)
            {
                Assert.Fail(@"Test failed for {0}: {1}

{2}",
                            propertyTypeToTest,
                            propertyValueToUse,
                            exception);
            }
        }

        [Test, Ignore("wondering how to simulate this?")]
        public void ThrowsIfTheSagaCannotBeSaved()
        {
            // arrange
            Assert.Fail("come up with a test");

            // act

            // assert
        }

        [Test]
        public void SagaDataHasProperMongolikePropertyNamesInDb()
        {
            // arrange
            var id = Guid.NewGuid();
            var currentRevision = 243;
            var nextRevision = currentRevision + 1;
            persister.Save(new SimpleSagaData { Id = id, Revision = currentRevision }, new string[0]);

            // act
            var mongoCollection = Collection<SimpleSagaData>("sagas");
            
            var simpleSagaDataById = mongoCollection.FindOne(Query.EQ("_id", id));

            var simpleSagaDataByIdAndRevision = mongoCollection
                .FindOne(Query.And(Query.EQ("_id", id),
                                   Query.EQ("_rev", nextRevision)));

            // assert
            simpleSagaDataById.ShouldNotBe(null);
            simpleSagaDataById.Id.ShouldBe(id);
            
            simpleSagaDataByIdAndRevision.ShouldNotBe(null);
            simpleSagaDataByIdAndRevision.Id.ShouldBe(id);
        }

        [Test]
        public void UsesOptimisticLockingAndDetectsRaceConditionsWhenUpdating()
        {
            // arrange
            var indexBySomeString = new[] { "SomeString" };
            var id = Guid.NewGuid();
            var simpleSagaData = new SimpleSagaData { Id = id, SomeString = "hello world!" };
            persister.Save(simpleSagaData, indexBySomeString);

            // act
            var sagaData1 = (SimpleSagaData)persister.Find("SomeString", "hello world!", typeof(SimpleSagaData));
            var sagaData2 = (SimpleSagaData)persister.Find("SomeString", "hello world!", typeof(SimpleSagaData));

            // assert
            persister.Save(sagaData1, indexBySomeString);
            var exception = Assert.Throws<OptimisticLockingException>(() => persister.Save(sagaData2, indexBySomeString));
            Console.WriteLine(exception);
        }

        [Test]
        public void PersistsSagaLikeExpected()
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
                                                   new SomeCollectedThing {No = 1},
                                                   new SomeCollectedThing {No = 2},
                                                   new SomeCollectedThing {No = 3},
                                                   new SomeCollectedThing {No = 4},
                                               }
                                       }
                    };

            persister.Save(complexPieceOfSagaData, new[] { "SomeField" });

            var mySagaData = Collection<MySagaData>("sagas").FindOne(Query.EQ("_id", sagaDataId));

            mySagaData.SomeField.ShouldBe("hello");
            mySagaData.AnotherField.ShouldBe("world!");
        }

        [Test]
        public void CanDeleteSaga()
        {
            // arrange
            var mySagaDataId = Guid.NewGuid();
            var mySagaData = new MySagaData
                                 {
                                     Id = mySagaDataId,
                                     SomeField = "whoolala"
                                 };

            Collection<MySagaData>("sagas").Insert(mySagaData);

            // act
            persister.Delete(mySagaData);

            // assert
            Collection<MySagaData>("sagas").FindOne(Query.EQ("_id", mySagaDataId)).ShouldBe(null);
        }

        [Test]
        public void CanFindSagaByPropertyValues()
        {
            // arrange
            Collection<MySagaData>("sagas").InsertBatch(new[]
                                                            {
                                                                SagaData(1, "some field 1"),
                                                                SagaData(2, "some field 2"),
                                                                SagaData(3, "some field 3"),
                                                            });

            // act
            var sagaDataType = typeof(MySagaData);
            var dataViaNonexistentValue = persister.Find("AnotherField", "non-existent value", sagaDataType);
            var dataViaNonexistentField = persister.Find("SomeFieldThatDoesNotExist", "doesn't matter", sagaDataType);
            var mySagaData = ((MySagaData)persister.Find("AnotherField", "some field 2", sagaDataType));
            
            // assert
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

        class MySagaData : ISagaData
        {
            public Guid Id { get; set; }

            public int Revision { get; set; }

            public string SomeField { get; set; }
            public string AnotherField { get; set; }
            public SomeEmbeddedThingie Embedded { get; set; }
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

        class SomeCollectedThing
        {
            public int No { get; set; }
        }

        class SimpleSagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
            public string SomeString { get; set; }
        }

        class GenericSagaData<T> : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
            public T Property { get; set; }
        }
    }
}