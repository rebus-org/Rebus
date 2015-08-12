using NUnit.Framework;
using Rebus.Tests.Contracts.Subscriptions;

namespace Rebus.Tests.Persistence.SqlServer
{
    [TestFixture, Category(Categories.SqlServer)]
    public class SqlServerSubscriptionStorageBasicSubscriptionOperations : BasicSubscriptionOperations<SqlServerSubscriptionStorageFactory>
    {
    }
}