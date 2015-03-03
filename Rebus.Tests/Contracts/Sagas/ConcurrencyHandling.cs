using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus2.Exceptions;
using Rebus2.Sagas;
using Tests;

namespace Rebus.Tests.Contracts.Sagas
{
    public class ConcurrencyHandling<TFactory> : FixtureBase where TFactory : ISagaStorageFactory, new()
    {
        ISagaStorage _sagaStorage;

        protected override void SetUp()
        {
            _sagaStorage = new TFactory().GetSagaStorage();
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