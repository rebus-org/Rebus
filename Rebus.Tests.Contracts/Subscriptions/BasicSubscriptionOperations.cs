using System.Linq;
using System.Threading.Tasks;
using Rebus.Subscriptions;
using Xunit;

namespace Rebus.Tests.Contracts.Subscriptions
{
    /// <summary>
    /// Test fixture base class for verifying compliance with the <see cref="ISubscriptionStorage"/> contract
    /// </summary>
    public abstract class BasicSubscriptionOperations<TSubscriptionStorageFactory> : FixtureBase where TSubscriptionStorageFactory : ISubscriptionStorageFactory, new()
    {
        ISubscriptionStorage _storage;
        TSubscriptionStorageFactory _factory;

        protected BasicSubscriptionOperations()
        {
            _factory = new TSubscriptionStorageFactory();
            _storage = _factory.Create();
        }

        protected override void TearDown()
        {
            _factory.Cleanup();
        }

        [Fact]
        public async Task StartsOutEmpty()
        {
            var subscribers = (await _storage.GetSubscriberAddresses("someTopic")).ToList();

            Assert.Equal(0, subscribers.Count);
        }

        [Fact]
        public async Task CanRegisterSameSubscriberMoreThanOnce()
        {
            await _storage.RegisterSubscriber("topic1", "subscriber1");
            await _storage.RegisterSubscriber("topic1", "subscriber1");
            await _storage.RegisterSubscriber("topic1", "subscriber1");
            await _storage.RegisterSubscriber("topic1", "subscriber1");
            await _storage.RegisterSubscriber("topic1", "subscriber1");
            await _storage.RegisterSubscriber("topic1", "subscriber1");

            var topic1Subscribers = (await _storage.GetSubscriberAddresses("topic1")).OrderBy(a => a).ToList();

            Assert.Equal(new[] { "subscriber1" }, topic1Subscribers);
        }

        [Fact]
        public async Task CanRegisterSubscribers()
        {
            await _storage.RegisterSubscriber("topic1", "subscriber1");
            await _storage.RegisterSubscriber("topic1", "subscriber2");
            await _storage.RegisterSubscriber("topic2", "subscriber1");

            var topic1Subscribers = (await _storage.GetSubscriberAddresses("topic1")).OrderBy(a => a).ToList();
            var topic2Subscribers = (await _storage.GetSubscriberAddresses("topic2")).OrderBy(a => a).ToList();

            Assert.Equal(new[] { "subscriber1", "subscriber2" }, topic1Subscribers);
            Assert.Equal(new[] { "subscriber1"}, topic2Subscribers);
        }

        [Fact]
        public async Task CanSubscribeAndUnsubscribe()
        {
            await _storage.RegisterSubscriber("topic1", "subscriber1");
            await _storage.RegisterSubscriber("topic1", "subscriber2");

            await _storage.RegisterSubscriber("topic2", "subscriber1");
            await _storage.RegisterSubscriber("topic3", "subscriber1");

            var subscriberCountBeforeUnsubscribing = (await _storage.GetSubscriberAddresses("topic1")).OrderBy(a => a).ToList();

            await _storage.UnregisterSubscriber("topic1", "subscriber1");

            var subscriberCountAfterUnsubscribing = (await _storage.GetSubscriberAddresses("topic1")).OrderBy(a => a).ToList();

            Assert.Equal(new[] { "subscriber1", "subscriber2" }, subscriberCountBeforeUnsubscribing);
            Assert.Equal(new[] { "subscriber2" }, subscriberCountAfterUnsubscribing);
        }
    }
}