// Copyright 2011 Mogens Heller Grabe
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software distributed under the License is
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.
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

        protected override void DoTearDown()
        {
            DropCollection("subscriptions");
        }

        [Test, Ignore("wondering how to simulate this?")]
        public void ThrowsIfSubscriptionCannotBeRemoved()
        {
            // arrange
            Assert.Fail("come up with a test");

            // act

            // assert
        }

        [Test, Ignore("wondering how to simulate this?")]
        public void ThrowsIfSubscriptionCannotBeAdded()
        {
            // arrange
            Assert.Fail("come up with test");

            // act

            // assert
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