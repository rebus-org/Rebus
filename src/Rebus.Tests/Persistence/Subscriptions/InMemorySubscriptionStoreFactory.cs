using Rebus.Persistence.InMemory;

namespace Rebus.Tests.Persistence.Subscriptions
{
    public class InMemorySubscriptionStoreFactory : ISubscriptionStoreFactory
    {
        public IStoreSubscriptions CreateStore()
        {
            return new InMemorySubscriptionStorage();
        }

        public void Dispose()
        {
        }
    }
}