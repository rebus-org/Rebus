using System;
using NUnit.Framework;
using Rebus.Extensions;
using Rebus.Tests.Persistence.Subscriptions.Factories;
using Shouldly;

namespace Rebus.Tests.Persistence.Subscriptions
{
    [TestFixture(typeof(InMemorySubscriptionStoreFactory))]
    [TestFixture(typeof(SqlServerSubscriptionStoreFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(PostgreSqlServerSubscriptionStoreFactory), Category = TestCategories.PostgreSql)]
    [TestFixture(typeof(RavenDbSubscriptionStoreFactory), Category = TestCategories.Raven)]
    [TestFixture(typeof(MongoDbSubscriptionStoreFactory), Category = TestCategories.Mongo)]
    [TestFixture(typeof(XmlSubscriptionStoreFactory))]
    public class TestSubscriptionStorage<TFactory> : FixtureBase where TFactory : ISubscriptionStoreFactory
    {
        TFactory factory;
        IStoreSubscriptions storage;

        protected override void DoSetUp()
        {
            factory = Activator.CreateInstance<TFactory>();
            storage = factory.CreateStore();
        }

        protected override void DoTearDown()
        {
            factory.Dispose();
        }

        [Test]
        public void CanRemoveSubscriptionsAsWell()
        {
            // arrange
            storage.Store(typeof(SomeMessageType), "some_sub");
            storage.Store(typeof(SomeMessageType), "another_sub");
            storage.Store(typeof(AnotherMessageType), "some_sub");

            // act
            storage.Remove(typeof(SomeMessageType), "some_sub");

            // assert
            var someMessageTypeSubscribers = storage.GetSubscribers(typeof(SomeMessageType));
            someMessageTypeSubscribers.Length.ShouldBe(1);
            someMessageTypeSubscribers[0].ShouldBe("another_sub");

            var anotherMessageTypeSubscribers = storage.GetSubscribers(typeof(AnotherMessageType));
            anotherMessageTypeSubscribers.Length.ShouldBe(1);
            anotherMessageTypeSubscribers[0].ShouldBe("some_sub");
        }

        [Test]
        public void StoresSubscriptionsLikeExpected()
        {
            // arrange
            storage.Store(typeof(SomeMessageType), "some_sub");
            storage.Store(typeof(SomeMessageType), "another_sub");
            storage.Store(typeof(AnotherMessageType), "yet_another_sub");

            // act
            var someSubscribers = storage.GetSubscribers(typeof(SomeMessageType));
            var anotherSubscribers = storage.GetSubscribers(typeof(AnotherMessageType));

            // assert
            someSubscribers.ShouldContain(e => e.In(new[] { "some_sub", "another_sub" }));
            anotherSubscribers.ShouldContain(e => e.In(new[] { "yet_another_sub" }));
        }

        [Test]
        public void DoesntThrowWhenTheresNoSubscriptionsForTheGivenMessageType()
        {
            // arrange

            // act
            var subscribers = storage.GetSubscribers(typeof(NeverSubscribeToThisOne_ItWillRuinTheTest));

            // assert
        }

        class NeverSubscribeToThisOne_ItWillRuinTheTest { }
    }

    public class SomeMessageType
    {
    }

    public class AnotherMessageType
    {
    }
}