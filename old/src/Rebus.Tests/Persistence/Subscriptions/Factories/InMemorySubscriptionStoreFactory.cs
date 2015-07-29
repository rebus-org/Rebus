using Rebus.Persistence.InMemory;

namespace Rebus.Tests.Persistence.Subscriptions.Factories
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