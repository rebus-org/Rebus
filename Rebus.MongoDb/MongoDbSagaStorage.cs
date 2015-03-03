using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Rebus2.Exceptions;
using Rebus2.Sagas;

namespace Rebus.MongoDb
{
    public class MongoDbSagaStorage : ISagaStorage
    {
        readonly MongoDatabase _mongoDatabase;
        readonly Func<Type, string> _collectionNameResolver;

        public MongoDbSagaStorage(MongoDatabase mongoDatabase, Func<Type, string> collectionNameResolver = null)
        {
            _mongoDatabase = mongoDatabase;
            _collectionNameResolver = collectionNameResolver ?? (type => type.Name);
        }

        public async Task<ISagaData> Find(Type sagaDataType, string propertyName, object propertyValue)
        {
            var collection = GetCollection(sagaDataType);

            return null;
        }

        public async Task Insert(ISagaData sagaData)
        {
            var collection = GetCollection(sagaData.GetType());
        }

        public async Task Update(ISagaData sagaData)
        {
            var collection = GetCollection(sagaData.GetType());
        }

        public async Task Delete(ISagaData sagaData)
        {
            var collection = GetCollection(sagaData.GetType());

            var result = collection.Remove(Query.EQ("_id", sagaData.Id));
            
            if (!result.UpdatedExisting)
            {
                throw new ConcurrencyException("Saga data with ID {0} in collection {1} could not be deleted because it was already removed",
                    sagaData.Id, collection.Name);
            }
        }

        MongoCollection GetCollection(Type sagaDataType)
        {
            try
            {
                var collectionName = _collectionNameResolver(sagaDataType);

                return _mongoDatabase.GetCollection<BsonDocument>(collectionName);
            }
            catch (Exception exception)
            {
                throw new ApplicationException(string.Format("Could not get MongoCollection for saga data of type {0}", sagaDataType));
            }
        }
    }
}
