using System;
using System.Transactions;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Rebus.MongoDb
{
    /// <summary>
    /// MongoDB implementation of Rebus' <see cref="IStoreSagaData"/>. Please note that MongoDB does
    /// not support two-phase commit, which instead gets simulated by enlisting in the ambient transaction
    /// when one is present, delaying the Save/Delete operation until the commit phase.
    /// </summary>
    public class MongoDbSagaPersister : IStoreSagaData
    {
        readonly string collectionName;
        readonly MongoDatabase database;
        
        bool indexCreated;

        public MongoDbSagaPersister(string connectionString, string collectionName)
        {
            this.collectionName = collectionName;
            database = MongoDatabase.Create(connectionString);
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

            // if an ambient TX is present, enlist the insert to be performed at commit time
            if (Transaction.Current != null)
            {
                var hack = new AmbientTxHack(() => collection.Save(sagaData, SafeMode.True));
                Transaction.Current.EnlistVolatile(hack, EnlistmentOptions.None);
            }
            else
            {
                collection.Save(sagaData, SafeMode.True);
            }
        }

        public void Delete(ISagaData sagaData)
        {
            var collection = database.GetCollection(collectionName);

            if (Transaction.Current != null)
            {
                var hack = new AmbientTxHack(() => collection.Remove(Query.EQ("_id", sagaData.Id)));
                Transaction.Current.EnlistVolatile(hack, EnlistmentOptions.None);
            }
            else
            {
                collection.Remove(Query.EQ("_id", sagaData.Id));
            }
        }

        public ISagaData Find(string sagaDataPropertyPath, string fieldFromMessage, Type sagaDataType)
        {
            var collection = database.GetCollection(sagaDataType, collectionName);

            var sagaData = collection.FindOneAs(sagaDataType, Query.EQ(sagaDataPropertyPath, fieldFromMessage));

            return (ISagaData) sagaData;
        }
    }
}
