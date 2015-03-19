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

        public const string StateProperty = "state";
        public const string LeaseProperty = "lease";
        readonly MongoCollection<BsonDocument> collection;

        /// <summary>
        /// Constructs the timeout storage, connecting to the Mongo database pointed to by the given connection string,
        /// storing the timeouts in the given collection
        /// </summary>
        public MongoDbTimeoutStorage(string connectionString, string collectionName)
        {
            var database = MongoHelper.GetDatabase(connectionString);
            collection = database.GetCollection(collectionName);
            collection.CreateIndex(IndexKeys.Ascending(DueMongoTimeout.TimeProperty), IndexOptions.SetBackground(true).SetUnique(false));
        }

        /// <summary>
        /// Adds the timeout to the underlying collection of timeouts
        /// </summary>
        public void Add(Timeout.Timeout newTimeout)
        {
            var doc = DueMongoTimeout.ToBsonDocument(newTimeout.ReplyTo, newTimeout.CorrelationId,
                newTimeout.TimeToReturn, newTimeout.SagaId,
                newTimeout.CustomData)
                .ToBsonDocument();

            collection.Insert(doc);
        }

        /// <summary>
        /// Queries the underlying timeout collection and returns due timeouts, removing them at the same time
        /// </summary>
        public DueTimeoutsResult GetDueTimeouts()
        {
            BsonDocument timeoutDocument;
            var boundTo = Guid.NewGuid();
            var lease = RebusTimeMachine.Now().AddMilliseconds(500);
            var timeoutDocuments = new List<BsonDocument>();
            do
            {
                timeoutDocument = collection.FindAndModify(new FindAndModifyArgs
                {
                    Query = Query.And(Query.LTE(DueMongoTimeout.TimeProperty, RebusTimeMachine.Now()),
                        Query.NE(StateProperty, boundTo), //make sure the same timeout is not picked over and over
                        Query.LTE(LeaseProperty, lease)), //under active lease
                    SortBy = SortBy.Ascending(DueMongoTimeout.TimeProperty),
                    Update = new UpdateBuilder()
                        .Set(StateProperty, boundTo)
                        .Set(LeaseProperty, lease)
                }).ModifiedDocument;

                timeoutDocuments.Add(timeoutDocument);
            } while (timeoutDocument != null);

            return new DueTimeoutsResult(timeoutDocuments.Where(bsonDocument => bsonDocument != null)
                .Select(bsonDocument => DueMongoTimeout.Create(bsonDocument, collection)));
        }

        static Guid GetGuid(BsonDocument doc, string propertyName)
        {
            return doc.Contains(propertyName) ? doc[propertyName].AsGuid : Guid.Empty;
        }

        static string GetString(BsonDocument doc, string propertyName)
        {
            return doc.Contains(propertyName) ? doc[propertyName].AsString : "";
        }

        class DueMongoTimeout : DueTimeout
        {
            readonly MongoCollection<BsonDocument> collection;
            readonly ObjectId objectId;

            public const string ReplyToProperty = "reply_to";
            public const string CorrIdProperty = "corr_id";
            public const string TimeProperty = "time";
            public const string SagaIdProperty = "saga_id";
            public const string DataProperty = "data";
            public const string IdProperty = "_id";

            public static BsonDocument ToBsonDocument(string replyTo, string correlationId, DateTime timeToReturn, Guid sagaId, string customData)
            {
                return new BsonDocument()
                .Add(CorrIdProperty, correlationId)
                .Add(SagaIdProperty, sagaId)
                .Add(TimeProperty, timeToReturn)
                .Add(DataProperty, customData)
                .Add(ReplyToProperty, replyTo);
            }

            public static DueMongoTimeout Create(BsonDocument timeoutBsonDocument, MongoCollection<BsonDocument> collection)
            {
                return new DueMongoTimeout(timeoutBsonDocument[ReplyToProperty].AsString,
                    GetString(timeoutBsonDocument, CorrIdProperty),
                    timeoutBsonDocument[TimeProperty].ToUniversalTime(),
                    GetGuid(timeoutBsonDocument, SagaIdProperty),
                    GetString(timeoutBsonDocument, DataProperty),
                    collection,
                    (ObjectId)timeoutBsonDocument[IdProperty]);
            }

            public DueMongoTimeout(string replyTo, string correlationId, DateTime timeToReturn, Guid sagaId, string customData, MongoCollection<BsonDocument> collection, ObjectId objectId)
                : base(replyTo, correlationId, timeToReturn, sagaId, customData)
            {
                this.collection = collection;
                this.objectId = objectId;
            }

            public override void MarkAsProcessed()
            {
                collection.Remove(Query.EQ(IdProperty, objectId));
            }
        }
    }
}