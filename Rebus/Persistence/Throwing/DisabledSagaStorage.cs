using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Sagas;

namespace Rebus.Persistence.Throwing
{
    class DisabledSagaStorage : ISagaStorage
    {
        public Task<ISagaData> Find(Type sagaDataType, string propertyName, object propertyValue)
        {
            throw new NotImplementedException();
        }

        public Task Insert(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            throw new NotImplementedException();
        }

        public Task Update(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            throw new NotImplementedException();
        }

        public Task Delete(ISagaData sagaData)
        {
            throw new NotImplementedException();
        }
    }
}