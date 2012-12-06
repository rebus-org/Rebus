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
            database = database = MongoHelper.GetDatabase(connectionString);
        }

        public void Store(Type messageType, string subscriberInputQueue)
        {
            var collection = database.GetCollection(collectionName);

            var criteria = Query.EQ("_id", messageType.FullName);
            var update = Update.AddToSet("endpoints", subscriberInputQueue);

            var safeModeResult = collection.Update(criteria, update, UpdateFlags.Upsert, WriteConcern.Acknowledged);

            EnsureResultIsGood(safeModeResult, "Adding {0} to {1} where _id is {2}",
                               subscriberInputQueue, collectionName, messageType.FullName);
        }

        public void Remove(Type messageType, string subscriberInputQueue)
        {
            var collection = database.GetCollection(collectionName);

            var criteria = Query.EQ("_id", messageType.FullName);
            var update = Update.Pull("endpoints", subscriberInputQueue);

            var safeModeResult = collection.Update(criteria, update, UpdateFlags.Upsert, WriteConcern.Acknowledged);

            EnsureResultIsGood(safeModeResult, "Removing {0} from {1} where _id is {2}",
                               subscriberInputQueue, collectionName, messageType.FullName);
        }

        public string[] GetSubscribers(Type messageType)
        {
            var collection = database.GetCollection(collectionName);

            var doc = collection.FindOne(Query.EQ("_id", messageType.FullName));
            if (doc == null) return new string[0];

            var bsonDocument = doc.AsBsonDocument;
            if (bsonDocument == null) return new string[0];

            var endpoints = bsonDocument["endpoints"].AsBsonArray;
            return endpoints.Values.Select(v => v.ToString()).ToArray();
        }

        void EnsureResultIsGood(WriteConcernResult writeConcernResult, string message, params object[] objs)
        {
            if (!writeConcernResult.Ok)
            {
                throw new ApplicationException(string.Format("The following operation didn't suceed: {0}", string.Format(message, objs)));
            }
        }
    }
}