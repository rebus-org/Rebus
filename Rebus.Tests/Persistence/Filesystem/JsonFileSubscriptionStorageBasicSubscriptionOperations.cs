using NUnit.Framework;
using Rebus.Tests.Contracts.Subscriptions;
using Rebus.Tests.Persistence.SqlServer;

namespace Rebus.Tests.Persistence.Filesystem
{
    [TestFixture]
    public class JsonFileSubscriptionStorageBasicSubscriptionOperations : BasicSubscriptionOperations<JsonFileSubscriptionStorageFactory>
    {
    }
}