using System;
using System.Threading.Tasks;
using Rebus.Subscriptions;

namespace Rebus.Persistence.Throwing;

class DisabledSubscriptionStorage : ISubscriptionStorage
{
    public Task<string[]> GetSubscriberAddresses(string topic) => throw GetException();

    public Task RegisterSubscriber(string topic, string subscriberAddress) => throw GetException();

    public Task UnregisterSubscriber(string topic, string subscriberAddress) => throw GetException();

    public bool IsCentralized => false;

    static InvalidOperationException GetException() => new InvalidOperationException(@"A subscription storage has not been configured. Please configure a subscription storage with the .Subscriptions(...) configurer, e.g. like so:

Configure.With(..)
    .(...)
    .Subscriptions(s => s.StoreInMemory())
    .(...)

in order to save subscriptions in memory, or something like 

Configure.With(..)
    .(...)
    .Subscriptions(s => s.StoreSqlServer(...))
    .(...)

if you have imported the Rebus.SqlServer package and want to store subscriptions in SQL Server.
");
}