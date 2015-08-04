using Rebus.Subscriptions;
using Rebus.Tests.Contracts.Subscriptions;

namespace Rebus.PostgreSql.Tests
{
    public class PostgreSqlSubscriptionStorageFactory : ISubscriptionStorageFactory
    {
        public PostgreSqlSubscriptionStorageFactory()
        {
            Cleanup();
        }

        public ISubscriptionStorage Create()
        {
            var subscriptionStorage = new PostgreSqlSubscriptionStorage(PostgreSqlTestHelper.ConnectionHelper, "subscriptions", true);
            subscriptionStorage.Initialize();
            return subscriptionStorage;
        }

        public void Cleanup()
        {
            PostgreSqlTestHelper.DropTable("subscriptions");
        }
    }
}