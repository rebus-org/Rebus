using System.Transactions;
using NUnit.Framework;
using Rebus.Persistence.SqlServer;
using System.Linq;
using Shouldly;

namespace Rebus.Tests.Persistence.SqlServer
{
    [TestFixture, Category(TestCategories.MsSql)]
    public class TestSqlServerSubscriptionStorage : DbFixtureBase
    {
        SqlServerSubscriptionStorage storage;

        protected override void DoSetUp()
        {
            storage = new SqlServerSubscriptionStorage(ConnectionString);
            DeleteRows("subscriptions");
        }

        [Test]
        public void SubscriptionsCanBeRemoved()
        {
            // arrange
            storage.Store(typeof(SomeType), "first_endpoint");            
            storage.Store(typeof(AnotherType), "first_endpoint");            
            storage.Store(typeof(SomeType), "second_endpoint");            

            // act
            storage.Remove(typeof(SomeType), "first_endpoint");

            // assert
            var someTypeSubscribers = storage.GetSubscribers(typeof(SomeType));
            someTypeSubscribers.Length.ShouldBe(1);
            someTypeSubscribers[0].ShouldBe("second_endpoint");

            var anotherTypeSubscribers = storage.GetSubscribers(typeof(AnotherType));
            anotherTypeSubscribers.Length.ShouldBe(1);
            anotherTypeSubscribers[0].ShouldBe("first_endpoint");
        }

        [Test]
        public void AddingSubscriptionEnlistsInAmbientTx()
        {
            using(var tx = new TransactionScope())
            {
                storage.Store(typeof(SomeType), "someEndpoint");

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
            storage.Store(typeof(SomeType), "sometype_subscriber");
            storage.Store(typeof(AnotherType), "anothertype_subscriber");
            storage.Store(typeof(ThirdType), "thirdtype_subscriber");

            var subscribers = storage.GetSubscribers(typeof(SomeType));
         
            subscribers.Length.ShouldBe(1);
            subscribers[0].ShouldBe("sometype_subscriber");
        }

        [Test]
        public void DoesNotThrowIfSameSubscriptionIsAddedMultipleTimes()
        {
            storage.Store(typeof(SomeType), "sometype_subscriber");
            storage.Store(typeof(SomeType), "sometype_subscriber");
            storage.Store(typeof(SomeType), "sometype_subscriber");
        }

        [Test]
        public void AddingSubscriptionIsIdempotent()
        {
            storage.Store(typeof(ThirdType), "thirdtype_subscriber");
            storage.Store(typeof(ThirdType), "thirdtype_subscriber");
            storage.Store(typeof(ThirdType), "thirdtype_subscriber");
            storage.Store(typeof(ThirdType), "thirdtype_subscriber");

            var subscribers = storage.GetSubscribers(typeof(ThirdType));

            subscribers.Length.ShouldBe(1);
        }

        class SomeType {}
        class AnotherType {}
        class ThirdType {}
    }
}