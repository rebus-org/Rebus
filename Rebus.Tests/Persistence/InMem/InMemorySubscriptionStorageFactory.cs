using NUnit.Framework;
using Rebus.Persistence.InMem;
using Rebus.Subscriptions;
using Rebus.Tests.Contracts.Subscriptions;

namespace Rebus.Tests.Persistence.InMem;

[TestFixture]
public class BasicSubscriptionOperations : BasicSubscriptionOperations<InMemorySubscriptionStorageFactory>
{
}

public class InMemorySubscriptionStorageFactory : ISubscriptionStorageFactory
{
    public ISubscriptionStorage Create()
    {
        return new InMemorySubscriptionStorage();
    }

    public void Cleanup()
    {
    }
}