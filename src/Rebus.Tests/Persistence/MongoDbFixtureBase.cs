using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MongoDB.Driver;
using NUnit.Framework;
using log4net.Config;

namespace Rebus.Tests.Persistence
{
    public abstract class MongoDbFixtureBase
    {
        MongoDatabase db;
        Process mongod;
        string datapath;

        static MongoDbFixtureBase()
        {
            XmlConfigurator.Configure();
        }

        public static string ConnectionString
        {
            get { return ConnectionStrings.MongoDb; }
        }

        [SetUp]
        public void SetUp()
        {
            TimeMachine.Reset();
            mongod = MongoHelper.StartServerFromScratch();
            db = MongoHelper.GetDatabase(ConnectionStrings.MongoDb);
            db.Drop();

            DoSetUp();
        }

        protected virtual void DoSetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
            DoTearDown();
            MongoHelper.StopServer(mongod, db);
        }

        protected virtual void DoTearDown()
        {
        }

        protected void DropCollection(string collectionName)
        {
            db.DropCollection(collectionName);
        }

        protected IEnumerable<string> GetCollectionNames()
        {
            return db.GetCollectionNames();
        }

        protected MongoCollection<T> Collection<T>(string collectionName)
        {
            return db.GetCollection<T>(collectionName);
        }
    }
}