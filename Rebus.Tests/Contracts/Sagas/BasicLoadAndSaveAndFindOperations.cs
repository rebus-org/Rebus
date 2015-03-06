using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Sagas;
using Rebus.Tests.Extensions;

namespace Rebus.Tests.Contracts.Sagas
{
    public abstract class BasicLoadAndSaveAndFindOperations<TFactory> : FixtureBase where TFactory : ISagaStorageFactory, new()
    {
        readonly IEnumerable<ISagaCorrelationProperty> NoCorrelationProperties = Enumerable.Empty<ISagaCorrelationProperty>();
        ISagaStorage _sagaStorage;
        TFactory _factory;

        protected override void SetUp()
        {
            _factory = new TFactory();
            _sagaStorage = _factory.GetSagaStorage();
        }

        protected override void TearDown()
        {
            _factory.Cleanup();
        }

        [Test]
        public async Task ThrowsIfIdHasNotBeenSet()
        {
            var sagaDataWithDefaultId = new AnotherSagaData { Id = Guid.Empty };

            Assert.Throws<InvalidOperationException>(async () => await _sagaStorage.Insert(sagaDataWithDefaultId, NoCorrelationProperties));
        }

        [Test]
        public async Task GetsNullWhenNoInstanceMatches()
        {
            var data = await _sagaStorage.Find(typeof(TestSagaData), "CorrelationId", "whatever");

            Assert.That(data, Is.Null);
        }

        [Test]
        public async Task GetsNullWhenPropertyDoesNotExist()
        {
            var data = await _sagaStorage.Find(typeof(TestSagaData), "NonExistingCorrelationId", "whatever");

            Assert.That(data, Is.Null);
        }

        [Test]
        public async Task GetsNullWhenValueDoesNotExist()
        {
            await _sagaStorage.Insert(new TestSagaData { Id = Guid.NewGuid(), CorrelationId = "existing" }, NoCorrelationProperties);

            var data = await _sagaStorage.Find(typeof(TestSagaData), "CorrelationId", "non-existing");

            Assert.That(data, Is.Null);
        }

        [Test]
        public async Task GetsTheInstanceWhenCorrelationPropertyMatches()
        {
            var sagaId = Guid.NewGuid();

            await _sagaStorage.Insert(new TestSagaData { Id = sagaId, CorrelationId = "existing" }, 
                CorrelationPropertiesFor<TestSagaData>(d => d.CorrelationId));

            var data = await _sagaStorage.Find(typeof(TestSagaData), "CorrelationId", "existing");

            Assert.That(data, Is.Not.Null);
            Assert.That(data.Id, Is.EqualTo(sagaId));
        }

        [Test]
        public async Task GetsNullWhenTheTypeDoesNotMatch()
        {
            var sagaId = Guid.NewGuid();

            await _sagaStorage.Insert(new TestSagaData { Id = sagaId, CorrelationId = "existing" },
                CorrelationPropertiesFor<TestSagaData>(d => d.CorrelationId));

            var data = await _sagaStorage.Find(typeof(AnotherSagaData), "CorrelationId", "existing");

            Assert.That(data, Is.Null);
        }

        [Test]
        public async Task GetsTheInstanceWhenIdPropertyMatches()
        {
            var sagaId = Guid.NewGuid();

            await _sagaStorage.Insert(new TestSagaData { Id = sagaId, CorrelationId = "existing" },
                CorrelationPropertiesFor<TestSagaData>(d => d.CorrelationId));

            var data = await _sagaStorage.Find(typeof(TestSagaData), "Id", sagaId);

            Assert.That(data, Is.Not.Null);
            Assert.That(data.Id, Is.EqualTo(sagaId));
        }

        [Test]
        public async Task NewlyInsertedSagaDataIsRevisionZero()
        {
            var sagaId = Guid.NewGuid();

            await _sagaStorage.Insert(new TestSagaData
            {
                Id = sagaId,
                Data = "yes, den kender jeg"
            }, NoCorrelationProperties);

            var loadedSagaData = await _sagaStorage.Find(typeof(TestSagaData), "Id", sagaId);

            Assert.That(loadedSagaData.Revision, Is.EqualTo(0));
        }

        [Test]
        public async Task RevisionIsIncrementedOnEachUpdate()
        {
            var sagaId = Guid.NewGuid();

            var initialTransientInstance = new TestSagaData { Id = sagaId, Data = "yes, den kender jeg" };

            Assert.That(initialTransientInstance.Revision, Is.EqualTo(0));

            await _sagaStorage.Insert(initialTransientInstance, NoCorrelationProperties);
            var loadedSagaData0 = await _sagaStorage.Find(typeof(TestSagaData), "Id", sagaId);
            
            Assert.That(loadedSagaData0.Revision, Is.EqualTo(0));
            Assert.That(initialTransientInstance.Revision, Is.EqualTo(0));

            await _sagaStorage.Update(loadedSagaData0, NoCorrelationProperties);
            var loadedSagaData1 = await _sagaStorage.Find(typeof(TestSagaData), "Id", sagaId);
            
            Assert.That(loadedSagaData0.Revision, Is.EqualTo(1));
            Assert.That(loadedSagaData1.Revision, Is.EqualTo(1));

            await _sagaStorage.Update(loadedSagaData1, NoCorrelationProperties);
            var loadedSagaData2 = await _sagaStorage.Find(typeof(TestSagaData), "Id", sagaId);
            
            Assert.That(loadedSagaData1.Revision, Is.EqualTo(2));
            Assert.That(loadedSagaData2.Revision, Is.EqualTo(2));
        }

        [Test]
        public async Task CanDeleteSagaData()
        {
            var sagaId = Guid.NewGuid();

            await _sagaStorage.Insert(new TestSagaData
            {
                Id = sagaId,
                Data = "yes, den kender jeg"
            }, NoCorrelationProperties);

            var loadedSagaData = await _sagaStorage.Find(typeof(TestSagaData), "Id", sagaId);

            Assert.That(loadedSagaData, Is.Not.Null);

            await _sagaStorage.Delete(loadedSagaData);

            var loadedSagaDataAfterDelete = await _sagaStorage.Find(typeof(TestSagaData), "Id", sagaId);

            Assert.That(loadedSagaDataAfterDelete, Is.Null);
        }


        class TestSagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
            public string CorrelationId { get; set; }
            public string Data { get; set; }
        }

        class AnotherSagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
        }

        IEnumerable<ISagaCorrelationProperty> CorrelationPropertiesFor<TSagaData>(params Expression<Func<TSagaData, object>>[] properties)
        {
            return properties
                .Select(Reflect.Path)
                .Select(propertyName => new CorrelationProperty(propertyName, typeof (TSagaData)));
        }

        class CorrelationProperty : ISagaCorrelationProperty
        {
            public CorrelationProperty(string propertyName, Type sagaDataType)
            {
                PropertyName = propertyName;
                SagaDataType = sagaDataType;
            }

            public string PropertyName { get; private set; }
            public Type SagaDataType { get; private set; }
        }
    }
}