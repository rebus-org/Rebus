using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Rebus.Exceptions;
using Rebus.Sagas;
using MongoDB.Bson.Serialization;

namespace Rebus.MongoDb.Sagas
{
    public class MongoDbSagaStorage : ISagaStorage
    {
        readonly IMongoDatabase _mongoDatabase;
        readonly Func<Type, string> _collectionNameResolver;

        public MongoDbSagaStorage(IMongoDatabase mongoDatabase, Func<Type, string> collectionNameResolver = null)
        {
            _mongoDatabase = mongoDatabase;
            _collectionNameResolver = collectionNameResolver ?? (type => type.Name);
        }

        public async Task<ISagaData> Find(Type sagaDataType, string propertyName, object propertyValue)
        {
            var collection = GetCollection(sagaDataType);

            if (propertyName == "Id") propertyName = "_id";

            var criteria = new BsonDocument(propertyName, BsonValue.Create(propertyValue));

            var result = await collection.Find(criteria).FirstOrDefaultAsync();

            return (ISagaData)BsonSerializer.Deserialize(result, sagaDataType);
        }

        public async Task Insert(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            if (sagaData.Id == Guid.Empty)
            {
                throw new InvalidOperationException(string.Format("Attempted to insert saga data {0} without an ID", sagaData.GetType()));
            }

            var collection = GetCollection(sagaData.GetType());

            await collection.InsertOneAsync(sagaData.ToBsonDocument());
        }

        public async Task Update(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            var collection = GetCollection(sagaData.GetType());

            var criteria = Builders<ISagaData>.Filter.And(Builders<ISagaData>.Filter.Eq(x => x.Id, sagaData.Id),
                Builders<ISagaData>.Filter.Eq(x => x.Revision, sagaData.Revision));

            sagaData.Revision++;

            var result = await collection.ReplaceOneAsync(criteria.ToBsonDocument(), sagaData.ToBsonDocument(sagaData.GetType()));

            if (!result.IsModifiedCountAvailable || result.ModifiedCount != 1)
            {
                throw new ConcurrencyException("Saga data {0} with ID {1} in collection {2} could not be updated!",
                    sagaData.GetType(), sagaData.Id, collection.CollectionNamespace);
            }
        }

        public async Task Delete(ISagaData sagaData)
        {
            var collection = GetCollection(sagaData.GetType());

            var result = await collection.DeleteManyAsync(new BsonDocument("_id", sagaData.Id));

            if (result.DeletedCount != 1)
            {
                throw new ConcurrencyException("Saga data {0} with ID {1} in collection {2} could not be deleted", 
                    sagaData.GetType(), sagaData.Id, collection.CollectionNamespace);
            }
        }

        IMongoCollection<BsonDocument> GetCollection(Type sagaDataType)
        {
            try
            {
                var collectionName = _collectionNameResolver(sagaDataType);

                return _mongoDatabase.GetCollection<BsonDocument>(collectionName).WithWriteConcern(WriteConcern.WMajority);
            }
            catch (Exception exception)
            {
                throw new ApplicationException(string.Format("Could not get MongoCollection for saga data of type {0}", sagaDataType), exception);
            }
        }
    }
}
