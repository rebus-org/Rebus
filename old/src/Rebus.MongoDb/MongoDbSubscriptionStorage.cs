using System;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Rebus.MongoDb
{
    /// <summary>
    /// MongoDB implementation of Rebus' <see cref="IStoreSubscriptions"/>. Will store subscriptions in one document per
    /// logical event type, keeping an array of subscriber endpoints inside that document. The document _id is
    /// the full name of the event type.
    /// </summary>
    public class MongoDbSubscriptionStorage : IStoreSubscriptions
    {
        readonly string collectionName;
        readonly MongoDatabase database;

        /// <summary>
        /// Constructs the storage to persist subscriptions in the given collection, in the database specified by the connection string.
        /// </summary>
        public MongoDbSubscriptionStorage(string connectionString, string collectionName)
        {
            this.collectionName = collectionName;
            database = database = MongoHelper.GetDatabase(connectionString);
        }

        /// <summary>
        /// Adds the given subscriber input queue to the collection of endpoints listed as subscribers of the given event type
        /// </summary>
        public void Store(Type eventType, string subscriberInputQueue)
        {
            var collection = database.GetCollection(collectionName);

            var criteria = Query.EQ("_id", eventType.FullName);
            var update = Update.AddToSet("endpoints", subscriberInputQueue);

            var safeModeResult = collection.Update(criteria, update, UpdateFlags.Upsert, WriteConcern.Acknowledged);

            EnsureResultIsGood(safeModeResult, "Adding {0} to {1} where _id is {2}",
                               subscriberInputQueue, collectionName, eventType.FullName);
        }

        /// <summary>
        /// Removes the given subscriber from the collection of endpoints listed as subscribers of the given event type
        /// </summary>
        public void Remove(Type eventType, string subscriberInputQueue)
        {
            var collection = database.GetCollection(collectionName);

            var criteria = Query.EQ("_id", eventType.FullName);
            var update = Update.Pull("endpoints", subscriberInputQueue);

            var safeModeResult = collection.Update(criteria, update, UpdateFlags.Upsert, WriteConcern.Acknowledged);

            EnsureResultIsGood(safeModeResult, "Removing {0} from {1} where _id is {2}",
                               subscriberInputQueue, collectionName, eventType.FullName);
        }

        /// <summary>
        /// Gets all subscriber for the given event type
        /// </summary>
        public string[] GetSubscribers(Type eventType)
        {
            var collection = database.GetCollection(collectionName);

            var doc = collection.FindOne(Query.EQ("_id", eventType.FullName));
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
                throw new ApplicationException(
                    string.Format("The following operation didn't suceed: {0} - the result was: {1}",
                                  string.Format(message, objs),
                                  writeConcernResult.ErrorMessage));
            }
        }
    }
}