using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Rebus.Timeout;
using System.Linq;

namespace Rebus.MongoDb
{
    /// <summary>
    /// Implementation of <see cref="IStoreTimeouts"/> that stores timeouts in a MongoDB
    /// </summary>
    public class MongoDbTimeoutStorage : IStoreTimeouts
    {
        readonly string collectionName;
        readonly MongoDatabase database;

        /// <summary>
        /// Constructs the timeout storage, connecting to the Mongo database pointed to by the given connection string,
        /// storing the timeouts in the given collection
        /// </summary>
        public MongoDbTimeoutStorage(string connectionString, string collectionName)
        {
            this.collectionName = collectionName;
            database = MongoHelper.GetDatabase(connectionString);
        }

        public void Add(Timeout.Timeout newTimeout)
        {
            var collection = database.GetCollection(collectionName);

            collection.Insert(new
                                  {
                                      corr_id = newTimeout.CorrelationId,
                                      saga_id = newTimeout.SagaId,
                                      time = newTimeout.TimeToReturn,
                                      data = newTimeout.CustomData,
                                      reply_to = newTimeout.ReplyTo,
                                  });
        }

        public IEnumerable<DueTimeout> GetDueTimeouts()
        {
            var collection = database.GetCollection(collectionName);
            
            var result = collection.Find(Query.LTE("time", RebusTimeMachine.Now()))
                                   .SetSortOrder(SortBy.Ascending("time"));

            return result
                .Select(r => new DueMongoTimeout(r["reply_to"].AsString,
                                                 r["corr_id"].AsString,
                                                 r["time"].AsDateTime,
                                                 r["saga_id"].AsGuid,
                                                 r["data"] != BsonNull.Value
                                                     ? r["data"].AsString
                                                     : "",
                                                 collection,
                                                 (ObjectId) r["_id"]));
        }

        class DueMongoTimeout : DueTimeout
        {
            readonly MongoCollection<BsonDocument> collection;
            readonly ObjectId objectId;

            public DueMongoTimeout(string replyTo, string correlationId, DateTime timeToReturn, Guid sagaId, string customData, MongoCollection<BsonDocument> collection, ObjectId objectId) 
                : base(replyTo, correlationId, timeToReturn, sagaId, customData)
            {
                this.collection = collection;
                this.objectId = objectId;
            }

            public override void MarkAsProcessed()
            {
                collection.Remove(Query.EQ("_id", objectId));
            }
        }
    }
}