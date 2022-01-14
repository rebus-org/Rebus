using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Config;
using Rebus.Sagas;

namespace Rebus.Tests.Extensions;

public static class TestDecoratorsExtensions
{
    public static void ForceSagaPersistenceToBeSlow(this OptionsConfigurer configurer, int delayMilliseconds = 200)
    {
        configurer.Decorate<ISagaStorage>(c => new SlowSagaStorageDecorator(c.Get<ISagaStorage>(), delayMilliseconds));
    }

    class SlowSagaStorageDecorator : ISagaStorage
    {
        readonly ISagaStorage _sagaStorage;
        readonly int _delayMilliseconds;

        public SlowSagaStorageDecorator(ISagaStorage sagaStorage, int delayMilliseconds)
        {
            _sagaStorage = sagaStorage;
            _delayMilliseconds = delayMilliseconds;
        }

        public async Task<ISagaData> Find(Type sagaDataType, string propertyName, object propertyValue)
        {
            return await _sagaStorage.Find(sagaDataType, propertyName, propertyValue);
        }

        public async Task Insert(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            await Task.Delay(_delayMilliseconds);
            await _sagaStorage.Insert(sagaData, correlationProperties);
        }

        public async Task Update(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            await Task.Delay(_delayMilliseconds);
            await _sagaStorage.Update(sagaData, correlationProperties);
        }

        public async Task Delete(ISagaData sagaData)
        {
            await Task.Delay(_delayMilliseconds);
            await _sagaStorage.Delete(sagaData);
        }
    }
}