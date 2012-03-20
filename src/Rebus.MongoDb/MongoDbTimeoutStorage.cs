using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Rebus.Timeout;

namespace Rebus.MongoDb
{
    public class MongoDbTimeoutStorage : IStoreTimeouts
    {
        readonly string collectionName;
        MongoDatabase database;

        public MongoDbTimeoutStorage(string connectionString, string collectionName)
        {
            this.collectionName = collectionName;

            database = MongoDatabase.Create(connectionString);
        }

        public void Add(Timeout.Timeout newTimeout)
        {
            var collection = database.GetCollection(collectionName);

            collection.Insert(new
                                  {
                                      corr_id = newTimeout.CorrelationId,
                                      time = newTimeout.TimeToReturn,
                                      data = newTimeout.CustomData,
                                      reply_to = newTimeout.ReplyTo,
                                  });
        }

        public IEnumerable<Timeout.Timeout> RemoveDueTimeouts()
        {
            var collection = database.GetCollection(collectionName);

            var timeout = collection.FindAndRemove(Query.LTE("time", Time.Now()), SortBy.Ascending("time"));

            var document = timeout.ModifiedDocument;

            if (document == null)
            {
                return new Timeout.Timeout[0];
            }

            return new[]
                       {
                           new Timeout.Timeout
                               {
                                   CorrelationId = document["corr_id"].AsString,
                                   CustomData = document["data"] != BsonNull.Value ? document["data"].AsString : "",
                                   ReplyTo = document["reply_to"].AsString,
                                   TimeToReturn = document["time"].AsDateTime,
                               }
                       };
        }
    }
}