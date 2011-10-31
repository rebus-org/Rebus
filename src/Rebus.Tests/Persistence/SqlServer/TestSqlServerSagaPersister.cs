using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Transactions;
using Massive;
using NUnit.Framework;
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