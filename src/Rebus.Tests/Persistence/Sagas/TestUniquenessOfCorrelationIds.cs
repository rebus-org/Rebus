using System;
using NUnit.Framework;
using Ponder;
using Rebus.Tests.Persistence.Sagas.Factories;

namespace Rebus.Tests.Persistence.Sagas
{
    [TestFixture(typeof(SqlServerSagaPersisterFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(MongoDbSagaPersisterFactory), Category = TestCategories.Mongo)]
    [TestFixture(typeof(RavenDbSagaPersisterFactory), Category = TestCategories.Raven)]
    public class TestUniquenessOfCorrelationIds<TFactory> : TestSagaPersistersBase<TFactory> where TFactory : ISagaPersisterFactory
    {
        [Test, Description("We don't allow two sagas to have the same value of a property that is used to correlate with incoming messages, " +
                           "because that would cause an ambiguity if an incoming message suddenly mathed two or more sagas... " +
                           "moreover, e.g. MongoDB would not be able to handle the message and update multiple sagas reliably because it doesn't have transactions.")]
        public void CannotInsertAnotherSagaWithDuplicateCorrelationId()
        {
            // arrange
            var theValue = "this just happens to be the same in two sagas";
            var firstSaga = new SomeSaga {Id = Guid.NewGuid(), SomeCorrelationId = theValue};
            var secondSaga = new SomeSaga {Id = Guid.NewGuid(), SomeCorrelationId = theValue};

            var pathsToIndex = new[] {Reflect.Path<SomeSaga>(s => s.SomeCorrelationId)};
            Persister.Insert(firstSaga, pathsToIndex);

            // act
            // assert
            Assert.Throws<OptimisticLockingException>(() => Persister.Insert(secondSaga, pathsToIndex));
        }

        [Test]
        public void CannotUpdateAnotherSagaWithDuplicateCorrelationId()
        {
            // arrange
            var theValue = "this just happens to be the same in two sagas";
            var firstSaga = new SomeSaga {Id = Guid.NewGuid(), SomeCorrelationId = theValue};
            var secondSaga = new SomeSaga {Id = Guid.NewGuid(), SomeCorrelationId = "other value"};

            var pathsToIndex = new[] {Reflect.Path<SomeSaga>(s => s.SomeCorrelationId)};
            Persister.Insert(firstSaga, pathsToIndex);
            Persister.Insert(secondSaga, pathsToIndex);

            // act
            // assert
            secondSaga.SomeCorrelationId = theValue;
            Assert.Throws<OptimisticLockingException>(() => Persister.Update(secondSaga, pathsToIndex));
        }

        [Test]
        public void CanUpdateSaga()
        {
            // arrange
            var theValue = "this just happens to be the same in two sagas";
            var firstSaga = new SomeSaga {Id = Guid.NewGuid(), SomeCorrelationId = theValue};

            var pathsToIndex = new[] {Reflect.Path<SomeSaga>(s => s.SomeCorrelationId)};
            Persister.Insert(firstSaga, pathsToIndex);

            Assert.DoesNotThrow(() => Persister.Update(firstSaga, pathsToIndex));
        }

        internal class SomeSaga : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }

            public string SomeCorrelationId { get; set; }
        }
    }
}