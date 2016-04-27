using NUnit.Framework;
using Rebus.Tests.Contracts.Timeouts;

namespace Rebus.RavenDb.Tests.Timeouts
{
    [TestFixture, Category(TestCategory.RavenDb)]
    public class RavenDbTimeoutManagerTest : BasicStoreAndRetrieveOperations<RavenDbTimoutManagerFactory>
    {
    }
}
