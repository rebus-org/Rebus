using NUnit.Framework;
using Rebus.Tests.Contracts.Subscriptions;

namespace Rebus.RavenDb.Tests.Subscriptions
{
    [TestFixture, Category(TestCategory.RavenDb)]
    public class RavenDbSubscriptionStorageTests : BasicSubscriptionOperations<RavenDbSubscriptionStorageFactory>
    {
    }
}
