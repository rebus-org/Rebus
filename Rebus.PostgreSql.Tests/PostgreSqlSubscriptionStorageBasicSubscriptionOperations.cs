using NUnit.Framework;
using Rebus.Tests.Contracts.Subscriptions;

namespace Rebus.PostgreSql.Tests
{
    [TestFixture, Category(TestCategory.Postgres)]
    public class PostgreSqlSubscriptionStorageBasicSubscriptionOperations : BasicSubscriptionOperations<PostgreSqlSubscriptionStorageFactory>
    {
    }
}
