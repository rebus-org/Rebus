using System;
using System.Data.SqlClient;
using Rebus.Persistence.SqlServer;
using log4net.Config;
using System.Linq;
using Rebus.PostgreSql;

namespace Rebus.Tests.Persistence.Subscriptions.Factories
{
    public class PostgreSqlServerSubscriptionStoreFactory : ISubscriptionStoreFactory
    {
        const string SubscriptionsTableName = "subscriptions";

        static PostgreSqlServerSubscriptionStoreFactory()
        {
            XmlConfigurator.Configure();
        }

        public void Dispose()
        {
        }

        public IStoreSubscriptions CreateStore()
        {
            if (PostgreSqlFixtureBase.GetTableNames()
                                    .Contains(SubscriptionsTableName, StringComparer.CurrentCultureIgnoreCase))
            {
                PostgreSqlFixtureBase.DropTable(SubscriptionsTableName);
            }
            
            return new PostgreSqlSubscriptionStorage(ConnectionStrings.PostgreSql, SubscriptionsTableName)
                .EnsureTableIsCreated();
        }
    }
}