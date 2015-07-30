using Rebus.Subscriptions;
using Rebus.Tests.Contracts.Subscriptions;

namespace Rebus.PostgreSql.Tests
{
    public class PostgreSqlSubscriptionStorageFactory : ISubscriptionStorageFactory
    {
        public ISubscriptionStorage Create()
        {
            return new PostgreSqlSubscriptionStorage();
        }

        public void Cleanup()
        {
            throw new System.NotImplementedException();
        }
    }
}