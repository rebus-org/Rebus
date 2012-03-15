using NUnit.Framework;
using Raven.Client.Embedded;
using Rebus.Extensions;
using Rebus.RavenDb;
using Shouldly;

namespace Rebus.Tests.Persistence.RavenDb
{
    [TestFixture, Category(TestCategories.Raven)]
    public class TestRavenDbSubscriptionStorage
    {
        RavenDbSubscriptionStorage storage;
        EmbeddableDocumentStore store;

        [SetUp]
        public void SetUp()
        {
            store = new EmbeddableDocumentStore
            {
                RunInMemory = true
            };
            store.Initialize();

            storage = new RavenDbSubscriptionStorage(store, "Subscriptions");
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

        class SomeMessageType {}
        class AnotherMessageType {}
    }
}