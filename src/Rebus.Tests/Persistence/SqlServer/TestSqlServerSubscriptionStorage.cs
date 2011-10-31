using System.Transactions;
using NUnit.Framework;
using Rebus.Persistence.SqlServer;
using System.Linq;

namespace Rebus.Tests.Persistence.SqlServer
{
    [TestFixture]
    public class TestSqlServerSubscriptionStorage : DbFixtureBase
    {
        SqlServerSubscriptionStorage storage;

        protected override void DoSetUp()
        {
            storage = new SqlServerSubscriptionStorage(ConnectionString);
            DeleteRows("subscriptions");
        }

        [Test]
        public void AddingSubscriptionEnlistsInAmbientTx()
        {
            using(var tx = new TransactionScope())
            {
                storage.Save(typeof(SomeType), "someEndpoint");

                // don't complete tx!
            }

            var subscribers = storage.GetSubscribers(typeof(SomeType));
            
            if (subscribers.Any())
            {
                Assert.Fail("Apparently, there was a subscription for SomeType: {0}", subscribers.First());
            }
        }

        [Test]
        public void CanSaveSubscriptions()
        {
            storage.Save(typeof(SomeType), "sometype_subscriber");
            storage.Save(typeof(AnotherType), "anothertype_subscriber");
            storage.Save(typeof(ThirdType), "thirdtype_subscriber");

            var subscribers = storage.GetSubscribers(typeof(SomeType));
            Assert.AreEqual(1, subscribers.Length);
            Assert.AreEqual("sometype_subscriber", subscribers[0]);
        }

        [Test]
        public void DoesNotThrowIfSameSubscriptionIsAddedMultipleTimes()
        {
            storage.Save(typeof(SomeType), "sometype_subscriber");
            storage.Save(typeof(SomeType), "sometype_subscriber");
            storage.Save(typeof(SomeType), "sometype_subscriber");
        }

        [Test]
        public void AddingSubscriptionIsIdempotent()
        {
            storage.Save(typeof(ThirdType), "thirdtype_subscriber");
            storage.Save(typeof(ThirdType), "thirdtype_subscriber");
            storage.Save(typeof(ThirdType), "thirdtype_subscriber");
            storage.Save(typeof(ThirdType), "thirdtype_subscriber");

            var subscribers = storage.GetSubscribers(typeof(ThirdType));

            Assert.AreEqual(1, subscribers.Length);
        }

        class SomeType {}
        class AnotherType {}
        class ThirdType {}
    }
}