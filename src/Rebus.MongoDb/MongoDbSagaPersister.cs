using System;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson;

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

        public void Save(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            var collection = database.GetCollection(collectionName);

            if (!indexCreated)
            {
                foreach (var propertyToIndex in sagaDataPropertyPathsToIndex)
                {
                    collection.EnsureIndex(IndexKeys.Ascending(propertyToIndex), IndexOptions.SetBackground(false));
                }
                indexCreated = true;
            }

            var criteria = Query.And(Query.EQ("_id", sagaData.Id),
                                     Query.EQ("_rev", sagaData.Revision));

            sagaData.Revision++;
            var update = Update.Replace(sagaData);
            SafeModeResult safeModeResult;
            try
            {
                safeModeResult = collection.Update(criteria, update, UpdateFlags.Upsert, SafeMode.True);
            }
            catch (MongoSafeModeException)
            {
                // in case of race conditions, we get a duplicate key error because the upsert
                // cannot proceed to insert a document with the same _id as an existing document
                // ... therefore, we map the MongoSafeModeException to our own OptimisticLockingException
                throw new OptimisticLockingException(sagaData);
            }

            EnsureResultIsGood(safeModeResult,
                               "save saga data of type {0} with _id {1} and _rev {2}",
                               sagaData.GetType(),
                               sagaData.Id,
                               sagaData.Revision);
        }

        public void Delete(ISagaData sagaData)
        {
            var collection = database.GetCollection(collectionName);

            var query = Query.And(Query.EQ("_id", sagaData.Id),
                                  Query.EQ("_rev", sagaData.Revision));

            var safeModeResult = collection.Remove(query, SafeMode.True);

            EnsureResultIsGood(safeModeResult,
                               "delete saga data of type {0} with _id {1} and _rev {2}",
                               sagaData.GetType(),
                               sagaData.Id,
                               sagaData.Revision);
        }

        public T Find<T>(string sagaDataPropertyPath, object fieldFromMessage) where T : ISagaData
        {
            var collection = database.GetCollection(typeof(T), collectionName);

            if (sagaDataPropertyPath == "Id")
                return collection.FindOneByIdAs<T>(ToBsonValue(fieldFromMessage));

            var query = Query.EQ(MapSagaDataPropertyPath(sagaDataPropertyPath, typeof(T)), ToBsonValue(fieldFromMessage));

            var sagaData = collection.FindOneAs<T>(query);
            return sagaData;
        }

        static BsonValue ToBsonValue(object fieldFromMessage)
        {
            return BsonValue.Create(fieldFromMessage);
        }

        string MapSagaDataPropertyPath(string sagaDataPropertyPath, Type sagaDataType)
        {
            var propertyInfo = sagaDataType.GetProperty(sagaDataPropertyPath, BindingFlags.Public | BindingFlags.Instance);

            if (propertyInfo == null)
                return sagaDataPropertyPath;

            return elementNameConventions.GetElementName(propertyInfo);
        }

        void EnsureResultIsGood(SafeModeResult safeModeResult, string message, params object[] objs)
        {
            if (!safeModeResult.Ok && safeModeResult.DocumentsAffected == 1)
            {
                var exceptionMessage = string.Format("Tried to {0}, but apparently the operation didn't succeed.",
                                                     string.Format(message, objs));
                
                throw new MongoSafeModeException(exceptionMessage, safeModeResult);
            }
        }
    }
}
