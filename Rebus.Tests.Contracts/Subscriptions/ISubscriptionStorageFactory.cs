using Rebus.Subscriptions;

namespace Rebus.Tests.Contracts.Subscriptions;

public interface ISubscriptionStorageFactory
{
    ISubscriptionStorage Create();
    void Cleanup();
}