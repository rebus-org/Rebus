using System;
using System.Threading.Tasks;
using Rebus2.Sagas;

namespace Rebus.MongoDb
{
    public class MongoDbSagaStorage : ISagaStorage
    {
        public async Task<ISagaData> Find(Type sagaDataType, string propertyName, object propertyValue)
        {
            throw new NotImplementedException();
        }

        public async Task Insert(ISagaData sagaData)
        {
            throw new NotImplementedException();
        }

        public async Task Update(ISagaData sagaData)
        {
            throw new NotImplementedException();
        }

        public async Task Delete(ISagaData sagaData)
        {
            throw new NotImplementedException();
        }
    }
}
