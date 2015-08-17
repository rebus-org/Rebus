using System;
using MongoDB.Bson;
using MongoDB.Driver;
using Rebus.Tests;

namespace Rebus.MongoDb.Tests
{
    public class MongoTestHelper
    {
        public static MongoUrl GetUrl()
        {
            var suffix = TestConfig.Suffix;

            var databaseName = string.Format("rebus2_test_{0}", suffix).TrimEnd('_');

            var mongoUrl = new MongoUrl(string.Format("mongodb://localhost/{0}", databaseName));

            Console.WriteLine("Using MongoDB {0}", mongoUrl);

            return mongoUrl;
        }

        public static IMongoDatabase GetMongoDatabase(IMongoClient mongoClient)
        {
            var url = GetUrl();
            var settings = new MongoDatabaseSettings
            {
                GuidRepresentation = GuidRepresentation.Standard,
                WriteConcern = WriteConcern.Acknowledged
            };
            var mongoDatabase = mongoClient.GetDatabase(url.DatabaseName, settings);
            return mongoDatabase;
        }

        public static IMongoClient GetMongoClient()
        {
            var url = GetUrl();

            var mongoClient = new MongoClient(url);

            return mongoClient;
        }
    }
}