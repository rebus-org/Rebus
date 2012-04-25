using System.Data.SqlClient;
using Rebus.Persistence.SqlServer;
using log4net.Config;

namespace Rebus.Tests.Persistence.Subscriptions
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
            DeleteRows("subscriptions");
            return new SqlServerSubscriptionStorage(SqlServerC.ConnectionString, "subscriptions");
        }

        protected void DeleteRows(string tableName)
        {
            ExecuteCommand("delete from " + tableName);
        }

        static void ExecuteCommand(string commandText)
        {
            using (var conn = new SqlConnection(SqlServerC.ConnectionString))
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