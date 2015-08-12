using System;
using System.Data.SqlClient;
using Rebus.Persistence.SqlServer;
using log4net.Config;
using System.Linq;

namespace Rebus.Tests.Persistence.Subscriptions.Factories
{
    public class SqlServerSubscriptionStoreFactory : ISubscriptionStoreFactory
    {
        static SqlServerSubscriptionStoreFactory()
        {
            XmlConfigurator.Configure();
        }

        public void Dispose()
        {
        }

        public IStoreSubscriptions CreateStore()
        {
            if (SqlServerFixtureBase.GetTableNames()
                                    .Contains("subscriptions", StringComparer.CurrentCultureIgnoreCase))
            {
                ExecuteCommand("drop table subscriptions");
            }
            return new SqlServerSubscriptionStorage(ConnectionStrings.SqlServer, "subscriptions")
                .EnsureTableIsCreated();
        }

        static void ExecuteCommand(string commandText)
        {
            using (var conn = new SqlConnection(ConnectionStrings.SqlServer))
            {
                conn.Open();

                using (var command = conn.CreateCommand())
                {
                    command.CommandText = commandText;
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}