using System;
using NUnit.Framework;
using Ponder;
using Rebus.Tests.Persistence.Sagas.Factories;
using Shouldly;

namespace Rebus.Tests.Persistence.Sagas
{
    [TestFixture(typeof(InMemorySagaPersisterFactory))]
    [TestFixture(typeof(SqlServerSagaPersisterFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(RavenDbSagaPersisterFactory), Category = TestCategories.Raven)]
    [TestFixture(typeof(MongoDbSagaPersisterFactory), Category = TestCategories.Mongo)]
    public class TestSagaPersisters<TFactory>: TestSagaPersistersBase<TFactory> where TFactory : ISagaPersisterFactory
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
            Persister.Save(savedSagaData, new[] { "Property" });

            var foundSagaData = Persister.Find<GenericSagaData<TProperty>>("Property", propertyValueToUse);

            foundSagaData.ShouldNotBe(null);
            foundSagaData.Id.ShouldBe(savedSagaDataId);
        }

        [Test]
        public void PersisterCanFindSagaById()
        {
            var savedSagaData = new MySagaData();
            var savedSagaDataId = Guid.NewGuid();
            savedSagaData.Id = savedSagaDataId;
            Persister.Save(savedSagaData, new string[0]);

            var foundSagaData = Persister.Find<MySagaData>("Id", savedSagaDataId);

            foundSagaData.ShouldNotBe(null);
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

            Persister.Save(complexPieceOfSagaData, new[] { "SomeField" });

            var sagaData = Persister.Find<MySagaData>("Id", sagaDataId);
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

            Persister.Save(mySagaData, new[] { "SomeString" });
            Persister.Delete(mySagaData);

            var sagaData = Persister.Find<SimpleSagaData>("Id", mySagaDataId);
            sagaData.ShouldBe(null);
        }

        [Test]
        public void CanFindSagaByPropertyValues()
        {
            Persister.Save(SagaData(1, "some field 1"), new[] { "AnotherField" });
            Persister.Save(SagaData(2, "some field 2"), new[] { "AnotherField" });
            Persister.Save(SagaData(3, "some field 3"), new[] { "AnotherField" });

            var dataViaNonexistentValue = Persister.Find<MySagaData>("AnotherField", "non-existent value");
            var dataViaNonexistentField = Persister.Find<MySagaData>("SomeFieldThatDoesNotExist", "doesn't matter");
            var mySagaData = Persister.Find<MySagaData>("AnotherField", "some field 2");

            dataViaNonexistentField.ShouldBe(null);
            dataViaNonexistentValue.ShouldBe(null);
            mySagaData.ShouldNotBe(null);
            mySagaData.SomeField.ShouldBe("2");
        }

       [Test]
       public void PersisterCanFindSagaDataWithNestedElements()
       {
           const string stringValue = "I expect to find something with this string!";
           var path = Reflect.Path<SagaDataWithNestedElement>(d => d.ThisOneIsNested.SomeString);

           Persister.Save(new SagaDataWithNestedElement
                              {
                                  Id = Guid.NewGuid(),
                                  Revision = 12,
                                  ThisOneIsNested = new ThisOneIsNested
                                                        {
                                                            SomeString = stringValue
                                                        }
                              }, new[] {path});

           var loadedSagaData = Persister.Find<SagaDataWithNestedElement>(path, stringValue);

           loadedSagaData.ShouldNotBe(null);
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