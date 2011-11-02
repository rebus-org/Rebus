using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Massive;
using NUnit.Framework;
using Ponder;
using Rebus.Persistence.SqlServer;

namespace Rebus.Tests.Persistence.SqlServer
{
    [TestFixture]
    public class TestSqlServerSagaPersister : DbFixtureBase
    {
        SqlServerSagaPersister persister;

        protected override void DoSetUp()
        {
            DeleteRows("sagas");
            DeleteRows("saga_index");
            persister = new SqlServerSagaPersister(ConnectionString);
        }

        [Test]
        public void CanStoreSagaAsExpected()
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
                persister.Save(complexPieceOfSagaData, new[] {"AnotherField", "Embedded.ThisIsEmbedded"});
                tx.Complete();
            }

            // look up saga by sagaDataId
            dynamic sagas = new DbSaga();
            dynamic saga = sagas.First(id: sagaDataId);

            var expectedJsonString = @"{
  ""$type"": ""Rebus.Tests.Persistence.SqlServer.TestSqlServerSagaPersister+MySagaData, Rebus.Tests"",
  ""Id"": ""{sagaDataId}"",
  ""SomeField"": ""hello"",
  ""AnotherField"": ""world!"",
  ""Embedded"": {
    ""$type"": ""Rebus.Tests.Persistence.SqlServer.TestSqlServerSagaPersister+SomeEmbeddedThingie, Rebus.Tests"",
    ""ThisIsEmbedded"": ""this is embedded"",
    ""Thingies"": {
      ""$type"": ""System.Collections.Generic.List`1[[Rebus.Tests.Persistence.SqlServer.TestSqlServerSagaPersister+SomeCollectedThing, Rebus.Tests]], mscorlib"",
      ""$values"": [
        {
          ""$type"": ""Rebus.Tests.Persistence.SqlServer.TestSqlServerSagaPersister+SomeCollectedThing, Rebus.Tests"",
          ""No"": 1
        },
        {
          ""$type"": ""Rebus.Tests.Persistence.SqlServer.TestSqlServerSagaPersister+SomeCollectedThing, Rebus.Tests"",
          ""No"": 2
        },
        {
          ""$type"": ""Rebus.Tests.Persistence.SqlServer.TestSqlServerSagaPersister+SomeCollectedThing, Rebus.Tests"",
          ""No"": 3
        },
        {
          ""$type"": ""Rebus.Tests.Persistence.SqlServer.TestSqlServerSagaPersister+SomeCollectedThing, Rebus.Tests"",
          ""No"": 4
        }
      ]
    }
  }
}".Replace("{sagaDataId}", sagaDataId.ToString());

            Assert.AreEqual(expectedJsonString, saga.data);

            dynamic index = new DbSagaIndex();
            var keys = Enumerable.ToList(index.Find(saga_id: sagaDataId, OrderBy: "[key]"));

            Assert.AreEqual(2, keys.Count);

            var first = keys[0];
            Assert.AreEqual("AnotherField", first.key);
            Assert.AreEqual(sagaDataId, first.saga_id);
            Assert.AreEqual("world!", first.value);

            var second = keys[1];
            Assert.AreEqual("Embedded.ThisIsEmbedded", second.key);
            Assert.AreEqual(sagaDataId, second.saga_id);
            Assert.AreEqual("this is embedded", second.value);
        }

        [Test]
        public void IssuesCorrectUpdateStatementIfSagaDataIsSavedMultipleTimes()
        {
            var sagaDataId = Guid.NewGuid();

            var sagaDataWithError = new MySagaData{AnotherField="hrllo", Id=sagaDataId};
            persister.Save(sagaDataWithError, new string[0]);

            var sagaDataCorrected = new MySagaData{AnotherField="hello", Id=sagaDataId};
            persister.Save(sagaDataCorrected, new string[0]);

            dynamic saga = new DbSaga();
            var sagaData = saga.First(id: sagaDataId);

            var expectedJson = @"{
  ""$type"": ""Rebus.Tests.Persistence.SqlServer.TestSqlServerSagaPersister+MySagaData, Rebus.Tests"",
  ""Id"": ""{sagaDataId}"",
  ""SomeField"": null,
  ""AnotherField"": ""hello"",
  ""Embedded"": null
}".Replace("{sagaDataId}", sagaDataId.ToString());

            Assert.AreEqual(expectedJson, sagaData.data);
        }

        [Test]
        public void CanFindSagaDataByLookingItUpInTheIndex()
        {
            // arrange
            var path1 = Reflect.Path<MySagaData>(d => d.SomeField);
            var path2 = Reflect.Path<MySagaData>(d => d.AnotherField);
            var path3 = Reflect.Path<MySagaData>(d => d.Embedded.ThisIsEmbedded);

            var sagaDataId = Guid.NewGuid();

            persister.Save(new MySagaData
                               {
                                   Id = sagaDataId,
                                   SomeField = "some value",
                                   AnotherField = "another field",
                                   Embedded = new SomeEmbeddedThingie
                                                  {
                                                      ThisIsEmbedded = "bla bla",
                                                      Thingies =
                                                          {
                                                              new SomeCollectedThing {No = 1},
                                                              new SomeCollectedThing {No = 2},
                                                              new SomeCollectedThing {No = 3},
                                                          }
                                                  }
                               },
                           new[] {path1, path2, path3});

            // act
            var sagaData1 = persister.Find(path1, "some value");
            var sagaData2 = persister.Find(path2, "another field");
            var sagaData3 = persister.Find(path3, "bla bla");

            // assert
            Assert.AreEqual(sagaDataId, sagaData1.Id);
            Assert.AreEqual(sagaDataId, sagaData2.Id);
            Assert.AreEqual(sagaDataId, sagaData3.Id);
        }

        [Test]
        public void CanDeleteSagaData()
        {
            // arrange
            var sagaDataId = Guid.NewGuid();

            var sagaData = new MySagaData
                               {
                                   Id = sagaDataId,
                                   SomeField = "some value",
                                   AnotherField = "another field",
                                   Embedded = new SomeEmbeddedThingie
                                                  {
                                                      ThisIsEmbedded = "bla bla",
                                                      Thingies =
                                                          {
                                                              new SomeCollectedThing {No = 1},
                                                              new SomeCollectedThing {No = 2},
                                                              new SomeCollectedThing {No = 3},
                                                          }
                                                  }
                               };

            persister.Save(sagaData, new string[0]);

            // act
            persister.Delete(sagaData);

            // assert
            dynamic saga = new DbSaga();
            int count = saga.Count(id: sagaDataId);

            Assert.AreEqual(0, count);
        }

        [Test]
        public void SavingSagaDataIsTransactional()
        {
            // arrange
            var sagaDataId = Guid.NewGuid();
            var sagaData = new MySagaData {Id = sagaDataId, SomeField = "some value"};

            // act
            using (var tx = new TransactionScope())
            {
                persister.Save(sagaData, new string[0]);
                
                // no complete!
            }

            // assert
            dynamic saga = new DbSaga();
            int count = saga.Count(id: sagaDataId);
            Assert.AreEqual(0, count);
        }

        class DbSaga : DynamicModel
        {
            public DbSaga() : base("LocalSqlServer", "sagas", "id")
            {
                ValidatesPresenceOf("id");
                ValidatesPresenceOf("data");
            }
        }

        class DbSagaIndex : DynamicModel
        {
            public DbSagaIndex() : base("LocalSqlServer", "saga_index", "id")
            {
                ValidatesPresenceOf("id");
                ValidatesPresenceOf("saga_id");
                ValidatesPresenceOf("key");
                ValidatesPresenceOf("value");
            }
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