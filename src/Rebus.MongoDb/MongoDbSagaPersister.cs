using System;
using System.Collections.Generic;
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
        static readonly SagaDataElementNameConvention ElementNameConventions;

        static MongoDbSagaPersister()
        {
            ElementNameConventions = new SagaDataElementNameConvention();

            var conventionProfile = new ConventionProfile()
                .SetElementNameConvention(ElementNameConventions);

            BsonClassMap.RegisterConventions(conventionProfile, t => typeof(ISagaData).IsAssignableFrom(t));
        }

        readonly Dictionary<Type, string> collectionNames = new Dictionary<Type, string>();
        readonly MongoDatabase database;

        volatile bool indexCreated;

        bool allowAutomaticSagaCollectionNames;

        public MongoDbSagaPersister(string connectionString)
        {
            database = MongoDatabase.Create(connectionString);
        }

        public MongoDbSagaPersister SetCollectionName<TSagaData>(string collectionName) where TSagaData : ISagaData
        {
            collectionNames.Add(typeof(TSagaData), collectionName);
            return this;
        }

        public void Insert(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            var collection = database.GetCollection(GetCollectionName(sagaData.GetType()));

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
            var collection = database.GetCollection(GetCollectionName(sagaData.GetType()));

            EnsureIndexHasBeenCreated(sagaDataPropertyPathsToIndex, collection);

            var revisionElementName = GetRevisionElementName(sagaData);

            var criteria = Query.And(Query.EQ("_id", sagaData.Id),
                                     Query.EQ(revisionElementName, sagaData.Revision));

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

        string GetCollectionName(Type sagaDataType)
        {
            if (collectionNames.ContainsKey(sagaDataType))
            {
                return collectionNames[sagaDataType];
            }

            if (allowAutomaticSagaCollectionNames)
            {
                return GenerateAutoSagaCollectionName(sagaDataType);
            }

            throw new InvalidOperationException(
                string.Format(
                    @"This MongoDB saga persister doesn't know where to store sagas of type {0}.

You must specify a collection for each saga type on the persister, e.g. like so:

    new MongoDbSagaPersister(ConnectionString)
        .SetCollectionName<MySagaData>(""my_sagas"")
        .SetCollectionName<MyOtherSagaData>(""my_other_sagas"");

if you create the persister manually, or like this if you're using the fluent configuration API:

    Configure.With(adapter)
        (...)
        .Sagas(s => s.StoreInMongoDb(ConnectionString)
                        .SetCollectionName<SomeKindOfSaga>(""some_kind_of_saga"")
                        .SetCollectionName<AnotherKindOfSaga>(""another_kind_of_saga""))
        (...)

Alternatively, if you're more into the ""convention over configuration"" thing, trade a little
bit of control for reduced friction, and let the persister come up with a name by itself:

    Configure.With(adapter)
        (...)
        .Sagas(s => s.StoreInMongoDb(ConnectionString)
                        .AllowAutomaticSagaCollectionNames())
        (...)

which will make the persister use the type of the saga to come up with collection names 
automatically - for sagas of the type {0}, the collection will be named '{1}'.
",
                    sagaDataType, GenerateAutoSagaCollectionName(sagaDataType)));
        }

        static string GenerateAutoSagaCollectionName(Type sagaDataType)
        {
            return string.Format("sagas_{0}", sagaDataType.Name);
        }

        void EnsureIndexHasBeenCreated(IEnumerable<string> sagaDataPropertyPathsToIndex, MongoCollection<BsonDocument> collection)
        {
            if (!indexCreated)
            {
                lock (this)
                {
                    if (!indexCreated)
                    {
                        collection.ResetIndexCache();

                        //                        var indexes = collection.GetIndexes();

                        foreach (var propertyToIndex in sagaDataPropertyPathsToIndex.Except(new[] { "Id" }))
                        {
                            var indexDefinition = IndexKeys.Ascending(propertyToIndex);

                            //if (IndexAlreadyExists(indexes, propertyToIndex))
                            //{
                            //    AssertIndexIsCorrect(indexes, propertyToIndex);
                            //}

                            collection.EnsureIndex(indexDefinition,
                                                   IndexOptions.SetBackground(false).SetUnique(true));
                        }

                        //collection.ReIndex();

                        indexCreated = true;
                    }
                }
            }
        }

        void AssertIndexIsCorrect(GetIndexesResult indexes, string propertyToIndex)
        {
            var indexInfo = indexes.Single(i => IsIndexForProperty(propertyToIndex, i));

            if (!indexInfo.IsUnique)
            {
                throw new InvalidOperationException(string.Format("The index for {0} already existed, but it wasn't enforcing a UNIQUE constraint.", propertyToIndex));
            }

            if (indexInfo.IsBackground)
            {
                throw new InvalidOperationException(string.Format("The index for {0} aready exists, but it wasn't SYNCHRONOUS."));
            }
        }

        static string GetRevisionElementName(ISagaData sagaData)
        {
            var revisionElementName = "_rev";

            var classmap = BsonClassMap.LookupClassMap(sagaData.GetType());
            var revision = classmap.GetMemberMap("Revision");
            if (revision != null)
            {
                revisionElementName = revision.ElementName;
            }

            return revisionElementName;
        }

        bool IndexAlreadyExists(IEnumerable<IndexInfo> indexes, string propertyToIndex)
        {
            return indexes
                .Any(indexInfo => IsIndexForProperty(propertyToIndex, indexInfo));
        }

        static bool IsIndexForProperty(string propertyToIndex, IndexInfo indexInfo)
        {
            var indexKeys = indexInfo.Key.ToDictionary();

            return indexKeys.Count == 1 && indexKeys.ContainsKey(propertyToIndex);
        }

        public void Delete(ISagaData sagaData)
        {
            var collection = database.GetCollection(GetCollectionName(sagaData.GetType()));

            var revisionElementName = GetRevisionElementName(sagaData);

            var query = Query.And(Query.EQ("_id", sagaData.Id),
                                  Query.EQ(revisionElementName, sagaData.Revision));

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

        public T Find<T>(string sagaDataPropertyPath, object fieldFromMessage) where T : class, ISagaData
        {
            var collection = database.GetCollection(typeof(T), GetCollectionName(typeof(T)));

            if (sagaDataPropertyPath == "Id")
                return collection.FindOneByIdAs<T>(BsonValue.Create(fieldFromMessage));

            var query = Query.EQ(MapSagaDataPropertyPath(sagaDataPropertyPath, typeof(T)), BsonValue.Create(fieldFromMessage));
            return collection.FindOneAs<T>(query);
        }

        string MapSagaDataPropertyPath(string sagaDataPropertyPath, Type sagaDataType)
        {
            var propertyInfo = sagaDataType.GetProperty(sagaDataPropertyPath, BindingFlags.Public | BindingFlags.Instance);

            if (propertyInfo == null)
                return sagaDataPropertyPath;

            return ElementNameConventions.GetElementName(propertyInfo);
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

        public MongoDbSagaPersister AllowAutomaticSagaCollectionNames()
        {
            allowAutomaticSagaCollectionNames = true;
            return this;
        }
    }
}
