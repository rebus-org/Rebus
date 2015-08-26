using NUnit.Framework;
using Rebus.Tests.Contracts.Subscriptions;

namespace Rebus.Tests.Persistence.SqlServer
{
    [TestFixture]
    public class JsonFileSubscriptionStorageBasicSubscriptionOperations : BasicSubscriptionOperations<JsonFileSubscriptionStorageFactory>
    {
    }
}