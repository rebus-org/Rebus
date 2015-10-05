using Rebus.Logging;
using Rebus.PostgreSql.Subscriptions;
using Rebus.Subscriptions;
using Rebus.Tests.Contracts.Subscriptions;

namespace Rebus.PostgreSql.Tests.Subscriptions
{
    public class PostgreSqlSubscriptionStorageFactory : ISubscriptionStorageFactory
    {
        public PostgreSqlSubscriptionStorageFactory()
        {
            Cleanup();
        }

        public ISubscriptionStorage Create()
        {
            var subscriptionStorage = new PostgreSqlSubscriptionStorage(PostgreSqlTestHelper.ConnectionHelper, "subscriptions", true, new ConsoleLoggerFactory(false));
            subscriptionStorage.EnsureTableIsCreated();
            return subscriptionStorage;
        }

        public void Cleanup()
        {
            PostgreSqlTestHelper.DropTable("subscriptions");
        }
    }
}