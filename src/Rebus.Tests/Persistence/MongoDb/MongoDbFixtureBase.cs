using System;
using System.Collections.Generic;
using MongoDB.Driver;
using NUnit.Framework;

namespace Rebus.Tests.Persistence.MongoDb
{
    public abstract class MongoDbFixtureBase
    {
        protected const string ConnectionString = "mongodb://localhost:27017/rebus_test";
        
        readonly HashSet<string> collectionsToDrop = new HashSet<string>();

        MongoDatabase db;

        [SetUp]
        public void SetUp()
        {
            DoSetUp();

            db = MongoDatabase.Create(ConnectionString);
            collectionsToDrop.Clear();
        }

        protected virtual void DoSetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
            DoTearDown();
        }

        protected virtual void DoTearDown()
        {
        }

        protected void DropCollection(string collectionName)
        {
            db.DropCollection(collectionName);
        }

        protected MongoCollection<T> Collection<T>(string collectionName)
        {
            collectionsToDrop.Add(collectionName);

            return db.GetCollection<T>(collectionName);
        }
    }
}