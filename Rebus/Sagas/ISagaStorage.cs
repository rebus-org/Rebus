using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rebus.Sagas
{
    public interface ISagaStorage
    {
        Task<ISagaData> Find(Type sagaDataType, string propertyName, object propertyValue);
        Task Insert(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties);
        Task Update(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties);
        Task Delete(ISagaData sagaData);
    }
}