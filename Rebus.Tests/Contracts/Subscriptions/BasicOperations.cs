using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Subscriptions;

namespace Rebus.Tests.Contracts.Subscriptions
{
    public class BasicOperations<TSubscriptionStorageFactory> : FixtureBase where TSubscriptionStorageFactory :ISubscriptionStorageFactory,new()
    {
        ISubscriptionStorage _storage;

        protected override void SetUp()
        {
            var factory = new TSubscriptionStorageFactory();
            _storage = factory.Create();
        }

        [Test]
        public async Task StartsOutEmpty()
        {
            var subscribers = (await _storage.GetSubscriberAddresses("someTopic")).ToList();

            Assert.That(subscribers.Count, Is.EqualTo(0));
        }
    }
}