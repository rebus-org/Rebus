using System;
using NUnit.Framework;
using Ponder;
using Rebus.Tests.Persistence.Sagas.Factories;

namespace Rebus.Tests.Persistence.Sagas
{
    [TestFixture(typeof(MongoDbSagaPersisterFactory), Category = TestCategories.Mongo)]
    [TestFixture(typeof(SqlServerSagaPersisterFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(RavenDbSagaPersisterFactory), Category = TestCategories.Raven)]
    [TestFixture(typeof(InMemorySagaPersisterFactory))]
    public class TestOptimisticConcurrency<TFactory> : TestSagaPersistersBase<TFactory> where TFactory : ISagaPersisterFactory
    {
        [Test]
        public void UsesOptimisticLockingAndDetectsRaceConditionsWhenUpdatingFindingBySomeProperty()
        {
            var indexBySomeString = new[] { "SomeString" };
            var id = Guid.NewGuid();
            var simpleSagaData = new SimpleSagaData { Id = id, SomeString = "hello world!" };
            persister.Insert(simpleSagaData, indexBySomeString);

            var sagaData1 = persister.Find<SimpleSagaData>("SomeString", "hello world!");
            sagaData1.SomeString = "I changed this on one worker";

            EnterAFakeMessageContext();

            var sagaData2 = persister.Find<SimpleSagaData>("SomeString", "hello world!");
            sagaData2.SomeString = "I changed this on another worker";
            persister.Update(sagaData2, indexBySomeString);

            ReturnToOriginalMessageContext();

            Assert.Throws<OptimisticLockingException>(() => persister.Insert(sagaData1, indexBySomeString));
        }

        [Test]
        public void UsesOptimisticLockingAndDetectsRaceConditionsWhenUpdatingFindingById()
        {
            var indexBySomeString = new[] { "Id" };
            var id = Guid.NewGuid();
            var simpleSagaData = new SimpleSagaData { Id = id, SomeString = "hello world!" };
            persister.Insert(simpleSagaData, indexBySomeString);

            var sagaData1 = persister.Find<SimpleSagaData>("Id", id);
            sagaData1.SomeString = "I changed this on one worker";

            EnterAFakeMessageContext();

            var sagaData2 = persister.Find<SimpleSagaData>("Id", id);
            sagaData2.SomeString = "I changed this on another worker";
            persister.Update(sagaData2, indexBySomeString);

            ReturnToOriginalMessageContext();

            Assert.Throws<OptimisticLockingException>(() => persister.Insert(sagaData1, indexBySomeString));
        }

        [Test]
        public void ConcurrentDeleteAndUpdateThrowsOnUpdate()
        {
            var indexBySomeString = new[] { "Id" };
            var id = Guid.NewGuid();
            var simpleSagaData = new SimpleSagaData { Id = id };

            persister.Insert(simpleSagaData, indexBySomeString);
            var sagaData1 = persister.Find<SimpleSagaData>("Id", id);
            sagaData1.SomeString = "Some new value";

            EnterAFakeMessageContext();
            var sagaData2 = persister.Find<SimpleSagaData>("Id", id);
            persister.Delete(sagaData2);
            ReturnToOriginalMessageContext();

            Assert.Throws<OptimisticLockingException>(() => persister.Update(sagaData1, indexBySomeString));
        }

        [Test]
        public void ConcurrentDeleteAndUpdateThrowsOnDelete()
        {
            var indexBySomeString = new[] { "Id" };
            var id = Guid.NewGuid();
            var simpleSagaData = new SimpleSagaData { Id = id };

            persister.Insert(simpleSagaData, indexBySomeString);
            var sagaData1 = persister.Find<SimpleSagaData>("Id", id);

            EnterAFakeMessageContext();
            var sagaData2 = persister.Find<SimpleSagaData>("Id", id);
            sagaData2.SomeString = "Some new value";
            persister.Update(sagaData2, indexBySomeString);
            ReturnToOriginalMessageContext();

            Assert.Throws<OptimisticLockingException>(() => persister.Delete(sagaData1));
        }

        [Test]
        public void InsertingTheSameSagaDataTwiceGeneratesAnError()
        {
            // arrange
            var sagaDataPropertyPathsToIndex = new[] { Reflect.Path<SimpleSagaData>(d => d.Id) };

            var sagaId = Guid.NewGuid();
            persister.Insert(new SimpleSagaData {Id = sagaId, Revision = 0, SomeString = "hello!"},
                             sagaDataPropertyPathsToIndex);

            // act
            // assert
            Assert.Throws<OptimisticLockingException>(
                () => persister.Insert(new SimpleSagaData {Id = sagaId, Revision = 0, SomeString = "hello!"},
                                       sagaDataPropertyPathsToIndex));
        }
    }
}