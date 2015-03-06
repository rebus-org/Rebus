using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Exceptions;
using Rebus.Sagas;

namespace Rebus.Tests.Contracts.Sagas
{
    public abstract class ConcurrencyHandling<TFactory> : FixtureBase where TFactory : ISagaStorageFactory, new()
    {
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
        public async Task ThrowsWhenRevisionDoesNotMatchExpected()
        {
            var id = Guid.NewGuid();

            await _sagaStorage.Insert(new SomeSagaData{Id = id});

            var loadedData1 = await _sagaStorage.Find(typeof(SomeSagaData), "Id", id);
            
            var loadedData2 = await _sagaStorage.Find(typeof(SomeSagaData), "Id", id);

            await _sagaStorage.Update(loadedData1);

            Assert.Throws<ConcurrencyException>(async () => await _sagaStorage.Update(loadedData2));
        }

        class SomeSagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
        }
    }
}