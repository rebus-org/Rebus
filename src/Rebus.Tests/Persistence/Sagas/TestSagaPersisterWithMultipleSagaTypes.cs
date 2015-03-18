using System;
using NUnit.Framework;
using Ponder;
using Rebus.Tests.Persistence.Sagas.Factories;
using Shouldly;

namespace Rebus.Tests.Persistence.Sagas
{
    [TestFixture(typeof(MongoDbSagaPersisterFactory), Category = TestCategories.Mongo)]
    [TestFixture(typeof(SqlServerSagaPersisterFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(RavenDbSagaPersisterFactory), Category = TestCategories.Raven)]
    [TestFixture(typeof(InMemorySagaPersisterFactory))]
    public class TestSagaPersisterWithMultipleSagaTypes<TFactory> : TestSagaPersistersBase<TFactory> where TFactory : ISagaPersisterFactory
    {
        [Test]
        public void CanInsertSagasOfMultipleTypes()
        {
            // arrange
            const string someString = "just happens to be the same in two otherwise unrelated sagas";
            var someFieldPathOne = Reflect.Path<OneKindOfSaga>(s => s.SomeField);
            var someFieldPathAnother = Reflect.Path<AnotherKindOfSaga>(s => s.SomeField);

            // act
            persister.Insert(new OneKindOfSaga { Id = Guid.NewGuid(), SomeField = someString }, new[] { "Id", someFieldPathOne });
            persister.Insert(new AnotherKindOfSaga { Id = Guid.NewGuid(), SomeField = someString }, new[] { "Id", someFieldPathAnother });

            var oneKindOfSagaLoaded = persister.Find<OneKindOfSaga>(someFieldPathOne, someString);
            var anotherKindOfSagaLoaded = persister.Find<AnotherKindOfSaga>(someFieldPathAnother, someString);

            // assert
            oneKindOfSagaLoaded.ShouldBeOfType<OneKindOfSaga>();
            anotherKindOfSagaLoaded.ShouldBeOfType<AnotherKindOfSaga>();
        }

        class OneKindOfSaga : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }

            public string SomeField { get; set; }
        }

        class AnotherKindOfSaga : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }

            public string SomeField { get; set; }
        }
    }
}