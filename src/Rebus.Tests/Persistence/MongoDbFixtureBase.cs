using MongoDB.Driver;
using NUnit.Framework;
using log4net.Config;

namespace Rebus.Tests.Persistence
{
    public abstract class MongoDbFixtureBase
    {
        static MongoDbFixtureBase()
        {
            XmlConfigurator.Configure();
        }

        MongoDatabase db;

        [SetUp]
        public void SetUp()
        {
            TimeMachine.Reset();

            DoSetUp();

            db = MongoDatabase.Create(ConnectionStrings.MongoDb);
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
            return db.GetCollection<T>(collectionName);
        }
    }
}