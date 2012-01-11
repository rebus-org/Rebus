using System;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Rebus.MongoDb
{
    public class MongoDbSubscriptionStorage : IStoreSubscriptions
    {
        readonly string collectionName;
        readonly MongoDatabase database;

        public MongoDbSubscriptionStorage(string connectionString, string collectionName)
        {
            this.collectionName = collectionName;

            database = MongoDatabase.Create(connectionString);
        }

        public void Store(Type messageType, string subscriberInputQueue)
        {
            var collection = database.GetCollection(collectionName);

            var criteria = Query.EQ("_id", messageType.FullName);
            var update = Update.AddToSet("endpoints", subscriberInputQueue);

            var safeModeResult = collection.Update(criteria, update, UpdateFlags.Upsert, SafeMode.True);

            EnsureResultIsGood(safeModeResult);
        }

        public void Remove(Type messageType, string subscriberInputQueue)
        {
            var collection = database.GetCollection(collectionName);

            var criteria = Query.EQ("_id", messageType.FullName);
            var update = Update.Pull("endpoints", subscriberInputQueue);

            var safeModeResult = collection.Update(criteria, update, UpdateFlags.Upsert, SafeMode.True);

            EnsureResultIsGood(safeModeResult);
        }

        public string[] GetSubscribers(Type messageType)
        {
            var collection = database.GetCollection(collectionName);

            var doc = collection.FindOne(Query.EQ("_id", messageType.FullName)).AsBsonDocument;
            if (doc == null) return new string[0];

            var endpoints = doc["endpoints"].AsBsonArray;
            return endpoints.Values.Select(v => v.ToString()).ToArray();
        }

        void EnsureResultIsGood(SafeModeResult safeModeResult)
        {
                
        }
    }
}