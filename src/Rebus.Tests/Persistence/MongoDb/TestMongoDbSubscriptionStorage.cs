using NUnit.Framework;
using Rebus.Extensions;
using Rebus.MongoDb;
using Shouldly;

namespace Rebus.Tests.Persistence.MongoDb
{
    [TestFixture, Category(TestCategories.Mongo)]
    public class TestMongoDbSubscriptionStorage : MongoDbFixtureBase
    {
        MongoDbSubscriptionStorage storage;

        protected override void DoSetUp()
        {
            storage = new MongoDbSubscriptionStorage(ConnectionString, "subscriptions");
        }

        [Test]
        public void StoresSubscriptionsLikeExpected()
        {
            // arrange
            storage.Save(typeof(SomeMessageType), "some_sub");
            storage.Save(typeof(SomeMessageType), "another_sub");
            storage.Save(typeof(AnotherMessageType), "yet_another_sub");

            // act
            var someSubscribers = storage.GetSubscribers(typeof (SomeMessageType));
            var anotherSubscribers = storage.GetSubscribers(typeof (AnotherMessageType));

            // assert
            someSubscribers.ShouldContain(e => e.In(new[]{"some_sub", "another_sub"}));
            anotherSubscribers.ShouldContain(e => e.In(new[]{"yet_another_sub"}));
        }

        class SomeMessageType {}
        class AnotherMessageType {}
    }
}