using System;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson;
using System.Linq;

namespace Rebus.MongoDb
{
    /// <summary>
    /// MongoDB implementation of Rebus' <see cref="IStoreSagaData"/>. Please note that MongoDB does
    /// not support two-phase commit, which instead gets simulated by enlisting in the ambient transaction
    /// when one is present, delaying the Save/Delete operation until the commit phase.
    /// </summary>
    public class MongoDbSagaPersister : IStoreSagaData
    {
        readonly SagaDataElementNameConvention elementNameConventions;
        readonly string collectionName;
        readonly MongoDatabase database;

        bool indexCreated;

        public MongoDbSagaPersister(string connectionString, string collectionName)
        {
            this.collectionName = collectionName;

            database = MongoDatabase.Create(connectionString);

            elementNameConventions = new SagaDataElementNameConvention();
            var conventionProfile = new ConventionProfile()
                .SetElementNameConvention(elementNameConventions);

            BsonClassMap.RegisterConventions(conventionProfile, t => typeof(ISagaData).IsAssignableFrom(t));
        }

        public void Insert(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            var collection = database.GetCollection(collectionName);

            EnsureIndexHasBeenCreated(sagaDataPropertyPathsToIndex, collection);

            sagaData.Revision++;
            try
            {
                var safeModeResult = collection.Insert(sagaData, SafeMode.True);

                EnsureResultIsGood(safeModeResult,
                       "insert saga data of type {0} with _id {1} and _rev {2}", 0,
                       sagaData.GetType(),
                       sagaData.Id,
                       sagaData.Revision);
            }
            catch (MongoSafeModeException ex)
            {
                // in case of race conditions, we get a duplicate key error because the upsert
                // cannot proceed to insert a document with the same _id as an existing document
                // ... therefore, we map the MongoSafeModeException to our own OptimisticLockingException
                throw new OptimisticLockingException(sagaData, ex);
            }
        }

        public void Update(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            var collection = database.GetCollection(collectionName);

            EnsureIndexHasBeenCreated(sagaDataPropertyPathsToIndex, collection);

            var criteria = Query.And(Query.EQ("_id", sagaData.Id),
                                     Query.EQ("_rev", sagaData.Revision));

            sagaData.Revision++;

            var update = MongoDB.Driver.Builders.Update.Replace(sagaData);
            try
            {
                var safeModeResult = collection.Update(criteria, update, SafeMode.True);

                EnsureResultIsGood(safeModeResult,
                       "update saga data of type {0} with _id {1} and _rev {2}", 1,
                       sagaData.GetType(),
                       sagaData.Id,
                       sagaData.Revision);
            }
            catch (MongoSafeModeException ex)
            {
                // in case of race conditions, we get a duplicate key error because the upsert
                // cannot proceed to insert a document with the same _id as an existing document
                // ... therefore, we map the MongoSafeModeException to our own OptimisticLockingException
                throw new OptimisticLockingException(sagaData, ex);
            }
        }

        void EnsureIndexHasBeenCreated(string[] sagaDataPropertyPathsToIndex, MongoCollection<BsonDocument> collection)
        {
            if (!indexCreated)
            {
                foreach (var propertyToIndex in sagaDataPropertyPathsToIndex.Except(new[]{"Id"}))
                {
                    collection.EnsureIndex(IndexKeys.Ascending(propertyToIndex), IndexOptions.SetBackground(false).SetUnique(true));
                }
                indexCreated = true;
            }
        }

        public void Delete(ISagaData sagaData)
        {
            var collection = database.GetCollection(collectionName);

            var query = Query.And(Query.EQ("_id", sagaData.Id),
                                  Query.EQ("_rev", sagaData.Revision));

            try
            {
                var safeModeResult = collection.Remove(query, SafeMode.True);

                EnsureResultIsGood(safeModeResult,
                       "delete saga data of type {0} with _id {1} and _rev {2}", 1,
                       sagaData.GetType(),
                       sagaData.Id,
                       sagaData.Revision);
            }
            catch (MongoSafeModeException ex)
            {
                // in case of race conditions, we get a duplicate key error because the upsert
                // cannot proceed to insert a document with the same _id as an existing document
                // ... therefore, we map the MongoSafeModeException to our own OptimisticLockingException
                throw new OptimisticLockingException(sagaData, ex);
            }
        }

        public T Find<T>(string sagaDataPropertyPath, object fieldFromMessage) where T : ISagaData
        {
            var collection = database.GetCollection(typeof(T), collectionName);

            if (sagaDataPropertyPath == "Id")
                return collection.FindOneByIdAs<T>(BsonValue.Create(fieldFromMessage));

            var query = Query.EQ(MapSagaDataPropertyPath(sagaDataPropertyPath, typeof(T)), BsonValue.Create(fieldFromMessage));

            var sagaData = collection.FindOneAs<T>(query);
            return sagaData;
        }

        string MapSagaDataPropertyPath(string sagaDataPropertyPath, Type sagaDataType)
        {
            var propertyInfo = sagaDataType.GetProperty(sagaDataPropertyPath, BindingFlags.Public | BindingFlags.Instance);

            if (propertyInfo == null)
                return sagaDataPropertyPath;

            return elementNameConventions.GetElementName(propertyInfo);
        }

        void EnsureResultIsGood(SafeModeResult safeModeResult, string message, int expectedNumberOfAffectedDocuments, params object[] objs)
        {
            if (!safeModeResult.Ok)
            {
                var exceptionMessage = string.Format("Tried to {0}, but apparently the operation didn't succeed.",
                                                     string.Format(message, objs));

                throw new MongoSafeModeException(exceptionMessage, safeModeResult);
            }

            if (safeModeResult.DocumentsAffected != expectedNumberOfAffectedDocuments)
            {
                var exceptionMessage = string.Format("Tried to {0}, but documents affected != {1}.",
                                                     string.Format(message, objs),
                                                     expectedNumberOfAffectedDocuments);

                throw new MongoSafeModeException(exceptionMessage, safeModeResult);
            }
        }
    }
}
