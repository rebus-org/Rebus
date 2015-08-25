using NUnit.Framework;
using Rebus.Tests.Contracts.Subscriptions;

namespace Rebus.RavenDb.Tests.Subscriptions
{
    [TestFixture]
    public class RavenDbSubscriptionStorageTests : BasicSubscriptionOperations<RavenDbSubscriptionStorageFactory>
    {
    }
}
