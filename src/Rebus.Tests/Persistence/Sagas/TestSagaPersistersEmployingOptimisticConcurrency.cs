using System;
using System.Linq;
using NUnit.Framework;
using Ponder;
using Rebus.Tests.Persistence.Sagas.Factories;

namespace Rebus.Tests.Persistence.Sagas
{
    [TestFixture(typeof(MongoDbSagaPersisterFactory), Category = TestCategories.Mongo)]
    [TestFixture(typeof(SqlServerSagaPersisterFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(RavenDbSagaPersisterFactory), Category = TestCategories.Raven)]
    public class TestSagaPersistersEmployingOptimisticConcurrency<TFactory> : TestSagaPersistersBase<TFactory> where TFactory : ISagaPersisterFactory
    {
        [Test]
        public void UsesOptimisticLockingAndDetectsRaceConditionsWhenUpdatingFindingBySomeProperty()
        {
            var indexBySomeString = new[] { "SomeString" };
            var id = Guid.NewGuid();
            var simpleSagaData = new SimpleSagaData { Id = id, SomeString = "hello world!" };
            Persister.Insert(simpleSagaData, indexBySomeString);

            var sagaData1 = Persister.Find<SimpleSagaData>("SomeString", "hello world!").Single();
            sagaData1.SomeString = "I changed this on one worker";

            EnterAFakeMessageContext();

            var sagaData2 = Persister.Find<SimpleSagaData>("SomeString", "hello world!").Single();
            sagaData2.SomeString = "I changed this on another worker";
            Persister.Update(sagaData2, indexBySomeString);

            ReturnToOriginalMessageContext();

            Assert.Throws<OptimisticLockingException>(() => Persister.Insert(sagaData1, indexBySomeString));
        }

        [Test]
        public void UsesOptimisticLockingAndDetectsRaceConditionsWhenUpdatingFindingById()
        {
            var indexBySomeString = new[] { "Id" };
            var id = Guid.NewGuid();
            var simpleSagaData = new SimpleSagaData { Id = id, SomeString = "hello world!" };
            Persister.Insert(simpleSagaData, indexBySomeString);

            var sagaData1 = Persister.Find<SimpleSagaData>("Id", id).Single();
            sagaData1.SomeString = "I changed this on one worker";

            EnterAFakeMessageContext();

            var sagaData2 = Persister.Find<SimpleSagaData>("Id", id).Single();
            sagaData2.SomeString = "I changed this on another worker";
            Persister.Update(sagaData2, indexBySomeString);

            ReturnToOriginalMessageContext();

            Assert.Throws<OptimisticLockingException>(() => Persister.Insert(sagaData1, indexBySomeString));
        }

        [Test]
        public void ConcurrentDeleteAndUpdateThrows()
        {
            var indexBySomeString = new[] { "Id" };
            var id = Guid.NewGuid();
            var simpleSagaData = new SimpleSagaData { Id = id };

            Persister.Insert(simpleSagaData, indexBySomeString);
            var sagaData1 = Persister.Find<SimpleSagaData>("Id", id).Single();
            sagaData1.SomeString = "Some new value";

            EnterAFakeMessageContext();
            var sagaData2 = Persister.Find<SimpleSagaData>("Id", id).Single();
            Persister.Delete(sagaData2);
            ReturnToOriginalMessageContext();

            Assert.Throws<OptimisticLockingException>(() => Persister.Update(sagaData1, indexBySomeString));
        }

        [Test]
        public void InsertingTheSameSagaDataTwiceGeneratesAnError()
        {
            // arrange
            var sagaDataPropertyPathsToIndex = new[] { Reflect.Path<SimpleSagaData>(d => d.Id) };

            var sagaId = Guid.NewGuid();
            Persister.Insert(new SimpleSagaData {Id = sagaId, Revision = 0, SomeString = "hello!"},
                             sagaDataPropertyPathsToIndex);

            // act
            // assert
            Assert.Throws<OptimisticLockingException>(
                () => Persister.Insert(new SimpleSagaData {Id = sagaId, Revision = 0, SomeString = "hello!"},
                                       sagaDataPropertyPathsToIndex));

        }
    }
}