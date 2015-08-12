using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Rebus.Exceptions;
using Rebus.Sagas;

namespace Rebus.MongoDb.Sagas
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

            if (propertyName == "Id") propertyName = "_id";

            var criteria = Query.EQ(propertyName, BsonValue.Create(propertyValue));

            var result = collection.FindOneAs(sagaDataType, new FindOneArgs {Query = criteria});

            return (ISagaData)result;
        }

        public async Task Insert(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            if (sagaData.Id == Guid.Empty)
            {
                throw new InvalidOperationException(string.Format("Attempted to insert saga data {0} without an ID", sagaData.GetType()));
            }

            var collection = GetCollection(sagaData.GetType());

            var result = collection.Insert(sagaData);

            try
            {
                CheckResult(result,0);
            }
            catch (Exception exception)
            {
                throw new ConcurrencyException(exception, "Saga data {0} with ID {1} in collection {2} could not be inserted!", 
                    sagaData.GetType(), sagaData.Id, collection.Name);
            }
        }

        public async Task Update(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            var collection = GetCollection(sagaData.GetType());

            var criteria = Query.And(
                Query.EQ("_id", sagaData.Id),
                Query.EQ("Revision", sagaData.Revision));

            sagaData.Revision++;

            var result = collection.Update(criteria, MongoDB.Driver.Builders.Update.Replace(sagaData));

            try
            {
                CheckResult(result, 1);
            }
            catch (Exception exception)
            {
                throw new ConcurrencyException(exception, "Saga data {0} with ID {1} in collection {2} could not be updated!",
                    sagaData.GetType(), sagaData.Id, collection.Name);
            }
        }

        public async Task Delete(ISagaData sagaData)
        {
            var collection = GetCollection(sagaData.GetType());

            var result = collection.Remove(Query.EQ("_id", sagaData.Id));

            try
            {
                CheckResult(result, 1);
            }
            catch (Exception exception)
            {
                throw new ConcurrencyException(exception, "Saga data {0} with ID {1} in collection {2} could not be deleted", 
                    sagaData.GetType(), sagaData.Id, collection.Name);
            }
        }

        void CheckResult(WriteConcernResult result, int expectedNumberOfAffectedDocuments)
        {
            if (!result.Ok)
            {
                throw new MongoWriteConcernException("Not OK result returned from the server", result);
            }

            if (result.DocumentsAffected != expectedNumberOfAffectedDocuments)
            {
                throw new MongoWriteConcernException(string.Format("DocumentsAffected != {0}", expectedNumberOfAffectedDocuments), result);
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
                throw new ApplicationException(string.Format("Could not get MongoCollection for saga data of type {0}", sagaDataType), exception);
            }
        }
    }
}
