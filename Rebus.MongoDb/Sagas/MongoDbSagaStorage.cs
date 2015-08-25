using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Rebus.Exceptions;
using Rebus.Sagas;
using MongoDB.Bson.Serialization;

namespace Rebus.MongoDb.Sagas
{
    /// <summary>
    /// Implementation of <see cref="ISagaStorage"/> that uses MongoDB to store saga data
    /// </summary>
    public class MongoDbSagaStorage : ISagaStorage
    {
        readonly IMongoDatabase _mongoDatabase;
        readonly Func<Type, string> _collectionNameResolver;

        /// <summary>
        /// Constructs the saga storage to use the given database. If specified, the given <paramref name="collectionNameResolver"/> will
        /// be used to get names for each type of saga data that needs to be persisted. By default, the saga data's <see cref="Type.Name"/>
        /// will be used.
        /// </summary>
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

            var result = await collection.Find(criteria).FirstOrDefaultAsync().ConfigureAwait(false);
            ISagaData sagaData = null;
            if (result != null)
            {
                sagaData = (ISagaData) BsonSerializer.Deserialize(result, sagaDataType);
            }

            return sagaData;
        }

        public async Task Insert(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            if (sagaData.Id == Guid.Empty)
            {
                throw new InvalidOperationException(string.Format("Attempted to insert saga data {0} without an ID", sagaData.GetType()));
            }

            var collection = GetCollection(sagaData.GetType());

            await collection.InsertOneAsync(sagaData.ToBsonDocument()).ConfigureAwait(false);
        }

        public async Task Update(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            var collection = GetCollection(sagaData.GetType());

            var criteria = Builders<BsonDocument>.Filter.And(Builders<BsonDocument>.Filter.Eq("_id", sagaData.Id),
                Builders<BsonDocument>.Filter.Eq("Revision", sagaData.Revision));

            sagaData.Revision++;

            var result = await collection.ReplaceOneAsync(criteria, sagaData.ToBsonDocument(sagaData.GetType())).ConfigureAwait(false);

            if (!result.IsModifiedCountAvailable || result.ModifiedCount != 1)
            {
                throw new ConcurrencyException("Saga data {0} with ID {1} in collection {2} could not be updated!",
                    sagaData.GetType(), sagaData.Id, collection.CollectionNamespace);
            }
        }

        public async Task Delete(ISagaData sagaData)
        {
            var collection = GetCollection(sagaData.GetType());

            var result = await collection.DeleteManyAsync(new BsonDocument("_id", sagaData.Id)).ConfigureAwait(false);

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

                return _mongoDatabase.GetCollection<BsonDocument>(collectionName);
            }
            catch (Exception exception)
            {
                throw new ApplicationException(string.Format("Could not get MongoCollection for saga data of type {0}", sagaDataType), exception);
            }
        }
    }
}
