using System;

namespace Rebus.Tests.Persistence.Subscriptions
{
    public interface ISubscriptionStoreFactory : IDisposable
    {
        IStoreSubscriptions CreateStore();
    }
}