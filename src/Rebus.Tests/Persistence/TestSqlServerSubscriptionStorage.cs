using NUnit.Framework;
using Rebus.Persistence.SqlServer;

namespace Rebus.Tests.Persistence
{
    [TestFixture]
    public class TestSqlServerSubscriptionStorage : DbFixtureBase
    {
        SqlServerSubscriptionStorage storage;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
        }

        [SetUp]
        public void SetUp()
        {
            storage = new SqlServerSubscriptionStorage(ConnectionString);
            DeleteRows("subscriptions");
        }

        [TearDown]
        public void TearDown()
        {
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

        class SomeType {}
        class AnotherType {}
        class ThirdType {}
    }
}