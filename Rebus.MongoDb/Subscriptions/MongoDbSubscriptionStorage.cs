using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Rebus.Subscriptions;

namespace Rebus.MongoDb.Subscriptions
{
    public class MongoDbSubscriptionStorage : ISubscriptionStorage
    {
        static readonly string[] NoSubscribers = new string[0];

        readonly MongoCollection<BsonDocument> _subscriptions;

        public MongoDbSubscriptionStorage(MongoDatabase database, string collectionName, bool isCentralized)
        {
            IsCentralized = isCentralized;
            _subscriptions = database.GetCollection<BsonDocument>(collectionName);
        }

        public async Task<string[]> GetSubscriberAddresses(string topic)
        {
            var doc = _subscriptions.FindOneById(topic);
            if (doc == null) return NoSubscribers;

            return doc["addresses"].AsBsonArray
                .Select(item => item.ToString())
                .ToArray();
        }

        public async Task RegisterSubscriber(string topic, string subscriberAddress)
        {
            _subscriptions.Update(Query.EQ("_id", topic),
                Update.AddToSet("addresses", subscriberAddress),
                UpdateFlags.Upsert);
        }

        public async Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
            _subscriptions.Update(Query.EQ("_id", topic),
                Update.Pull("addresses", subscriberAddress),
                UpdateFlags.Upsert);
        }

        public bool IsCentralized { get; private set; }
    }
}