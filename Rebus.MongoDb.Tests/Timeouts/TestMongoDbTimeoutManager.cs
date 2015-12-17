using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using Rebus.Logging;
using Rebus.MongoDb.Timeouts;
using Rebus.Tests;
using Rebus.Tests.Contracts.Timeouts;
using Rebus.Timeouts;

namespace Rebus.MongoDb.Tests.Timeouts
{
    [TestFixture, Category(MongoTestHelper.TestCategory)]
    public class TestMongoDbTimeoutManager : BasicStoreAndRetrieveOperations<MongoDbTimeoutManagerFactory>
    {
    }

    public class MongoDbTimeoutManagerFactory : ITimeoutManagerFactory
    {
        readonly IMongoDatabase _mongoDatabase;
        readonly string _collectionName = $"timeouts_{TestConfig.Suffix}";

        public MongoDbTimeoutManagerFactory()
        {
            _mongoDatabase = MongoTestHelper.GetMongoDatabase();
            DropCollection(_collectionName);
        }
        
        public ITimeoutManager Create()
        {
            return new MongoDbTimeoutManager(_mongoDatabase, _collectionName, new ConsoleLoggerFactory(false));
        }

        public void Cleanup()
        {
            DropCollection(_collectionName);
        }

        public string GetDebugInfo()
        {
            var docStrings = _mongoDatabase
                .GetCollection<BsonDocument>(_collectionName)
                .FindAsync(d => true)
                .Result
                .ToListAsync()
                .Result
                .Select(FormatDocument);

            return string.Join(Environment.NewLine, docStrings);
        }

        static string FormatDocument(BsonDocument document)
        {
            return document.ToString();
        }

        void DropCollection(string collectionName)
        {
            _mongoDatabase.DropCollectionAsync(collectionName).Wait();
        }
    }
}