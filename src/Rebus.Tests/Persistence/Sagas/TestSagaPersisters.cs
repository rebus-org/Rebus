using System;
using System.Linq;
using NUnit.Framework;
using Ponder;
using Rebus.Tests.Persistence.Sagas.Factories;
using Shouldly;

namespace Rebus.Tests.Persistence.Sagas
{
    [TestFixture(typeof(InMemorySagaPersisterFactory))]
    [TestFixture(typeof(MongoDbSagaPersisterFactory), Category = TestCategories.Mongo)]
    [TestFixture(typeof(SqlServerSagaPersisterFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(RavenDbSagaPersisterFactory), Category = TestCategories.Raven)]
    public class TestSagaPersisters<TFactory> : TestSagaPersistersBase<TFactory> where TFactory : ISagaPersisterFactory
    {
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
            Persister.Insert(savedSagaData, new[] { "Property" });

            var foundSagaData = Persister.Find<GenericSagaData<TProperty>>("Property", propertyValueToUse).Single();

            foundSagaData.Id.ShouldBe(savedSagaDataId);
        }

        [Test]
        public void PersisterCanFindSagaById()
        {
            var savedSagaData = new MySagaData();
            var savedSagaDataId = Guid.NewGuid();
            savedSagaData.Id = savedSagaDataId;
            Persister.Insert(savedSagaData, new string[0]);

            var foundSagaData = Persister.Find<MySagaData>("Id", savedSagaDataId).Single();

            foundSagaData.Id.ShouldBe(savedSagaDataId);
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

            Persister.Insert(complexPieceOfSagaData, new[] { "SomeField" });

            var sagaData = Persister.Find<MySagaData>("Id", sagaDataId).Single();
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

            Persister.Insert(mySagaData, new[] { "SomeString" });
            Persister.Delete(mySagaData);

            var sagaData = Persister.Find<SimpleSagaData>("Id", mySagaDataId);
            sagaData.ShouldBeEmpty();
        }

        [Test]
        public void CanFindSagaByPropertyValues()
        {
            Persister.Insert(SagaData(1, "some field 1"), new[] { "AnotherField" });
            Persister.Insert(SagaData(2, "some field 2"), new[] { "AnotherField" });
            Persister.Insert(SagaData(3, "some field 3"), new[] { "AnotherField" });

            var dataViaNonexistentValue = Persister.Find<MySagaData>("AnotherField", "non-existent value");
            var dataViaNonexistentField = Persister.Find<MySagaData>("SomeFieldThatDoesNotExist", "doesn't matter");
            var mySagaData = Persister.Find<MySagaData>("AnotherField", "some field 2");

            dataViaNonexistentField.ShouldBeEmpty();
            dataViaNonexistentValue.ShouldBeEmpty();
            mySagaData.Single().SomeField.ShouldBe("2");
        }

        [Test]
        public void SamePersisterCanSaveMultipleTypesOfSagaDatas()
        {
            var sagaId1 = Guid.NewGuid();
            var sagaId2 = Guid.NewGuid();
            Persister.Insert(new SimpleSagaData { Id = sagaId1, SomeString = "Olé" }, new[] { "Id" });
            Persister.Insert(new MySagaData { Id = sagaId2, AnotherField = "Yipiie" }, new[] { "Id" });

            var saga1 = Persister.Find<SimpleSagaData>("Id", sagaId1);
            var saga2 = Persister.Find<MySagaData>("Id", sagaId2);

            saga1.Single().SomeString.ShouldBe("Olé");
            saga2.Single().AnotherField.ShouldBe("Yipiie");
        }

       [Test]
       public void PersisterCanFindSagaDataWithNestedElements()
       {
           const string stringValue = "I expect to find something with this string!";
           var path = Reflect.Path<SagaDataWithNestedElement>(d => d.ThisOneIsNested.SomeString);

           Persister.Insert(new SagaDataWithNestedElement
                              {
                                  Id = Guid.NewGuid(),
                                  Revision = 12,
                                  ThisOneIsNested = new ThisOneIsNested
                                                        {
                                                            SomeString = stringValue
                                                        }
                              }, new[] {path});

           var loadedSagaData = Persister.Find<SagaDataWithNestedElement>(path, stringValue).Single();

           loadedSagaData.ThisOneIsNested.ShouldNotBe(null);
           loadedSagaData.ThisOneIsNested.SomeString.ShouldBe(stringValue);
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

        class SagaDataWithNestedElement : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
            public ThisOneIsNested ThisOneIsNested { get; set; }
        }

        class ThisOneIsNested
        {
            public string SomeString { get; set; }
        }
    }
}