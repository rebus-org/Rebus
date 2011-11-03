using System;
using System.Collections.Generic;
using System.Transactions;
using MongoDB.Driver.Builders;
using NUnit.Framework;
using Rebus.MongoDb;
using Shouldly;

namespace Rebus.Tests.Persistence.MongoDb
{
    [TestFixture, Category("mongo")]
    public class TestMongoDbSagaPersister : MongoDbFixtureBase
    {
        MongoDbSagaPersister persister;

        protected override void DoSetUp()
        {
            persister = new MongoDbSagaPersister(ConnectionString, "sagas");
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

            using (var tx = new TransactionScope())
            {
                persister.Save(complexPieceOfSagaData, new[] {"SomeField"});

                tx.Complete();
            }

            var mySagaData = Collection<MySagaData>("sagas").FindOne(Query.EQ("_id", sagaDataId));

            mySagaData.SomeField.ShouldBe("hello");
            mySagaData.AnotherField.ShouldBe("world!");
        }

        [Test]
        public void SavingSagaDataIsTransactional()
        {
            // arrange
            var sagaDataId = Guid.NewGuid();
            var sagaData = new MySagaData { Id = sagaDataId, SomeField = "some value" };

            // act
            using (var tx = new TransactionScope())
            {
                persister.Save(sagaData, new string[0]);

                // no complete!
            }

            // assert
            Collection<MySagaData>("sagas").FindOne(Query.EQ("_id", sagaDataId)).ShouldBe(null);
        }

        [Test]
        public void CanDeleteSaga()
        {
            // arrange
            var mySagaDataId = Guid.NewGuid();
            var mySagaData = new MySagaData
                                 {
                                     Id = mySagaDataId,
                                     SomeField="whoolala"
                                 };

            Collection<MySagaData>("sagas").Insert(mySagaData);

            // act
            persister.Delete(mySagaData);

            // assert
            Collection<MySagaData>("sagas").FindOne(Query.EQ("_id", mySagaDataId)).ShouldBe(null);
        }

        [Test]
        public void DeleteIsTransactional()
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
            using (var tx = new TransactionScope())
            {
                persister.Delete(mySagaData);
            
                // no complete!
            }

            // assert
            Collection<MySagaData>("sagas").FindOne(Query.EQ("_id", mySagaDataId)).ShouldNotBe(null);
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
            persister.Find("AnotherField", "non-existent value", sagaDataType).ShouldBe(null);
            persister.Find("SomeFieldThatDoesNotExist", "doesn't matter", sagaDataType).ShouldBe(null);
            ((MySagaData)persister.Find("AnotherField", "some field 2", sagaDataType)).SomeField.ShouldBe("2");
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
    }
}