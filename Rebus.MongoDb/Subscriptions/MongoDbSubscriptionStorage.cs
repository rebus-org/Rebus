using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Rebus.Subscriptions;

namespace Rebus.MongoDb.Subscriptions
{
    public class MongoDbSubscriptionStorage : ISubscriptionStorage
    {
        static readonly string[] NoSubscribers = new string[0];

        readonly IMongoCollection<BsonDocument> _subscriptions;

        public MongoDbSubscriptionStorage(IMongoDatabase database, string collectionName, bool isCentralized)
        {
            IsCentralized = isCentralized;
            _subscriptions = database.GetCollection<BsonDocument>(collectionName);
        }

        public async Task<string[]> GetSubscriberAddresses(string topic)
        {
            var doc = await _subscriptions.Find(new BsonDocument("_id", topic)).FirstOrDefaultAsync();

            if (doc == null) return NoSubscribers;

            return doc["addresses"].AsBsonArray
                .Select(item => item.ToString())
                .ToArray();
        }

        public async Task RegisterSubscriber(string topic, string subscriberAddress)
        {
             await _subscriptions.UpdateOneAsync(new BsonDocument("_id", topic),
                Builders<BsonDocument>.Update.AddToSet("addresses", subscriberAddress),
                new UpdateOptions() { IsUpsert = true });
        }

        public async Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
            await _subscriptions.UpdateOneAsync(new BsonDocument("_id", topic),
                 Builders<BsonDocument>.Update.Pull("addresses", subscriberAddress),
                new UpdateOptions() { IsUpsert = true });
        }

        public bool IsCentralized { get; private set; }
    }
}