using System;
using System.Threading.Tasks;

namespace Rebus2.Sagas
{
    public interface ISagaStorage
    {
        Task<ISagaData> Find(Type sagaDataType, string propertyName, object propertyValue);
        Task Insert(ISagaData sagaData);
        Task Update(ISagaData sagaData);
        Task Delete(ISagaData sagaData);
    }
}