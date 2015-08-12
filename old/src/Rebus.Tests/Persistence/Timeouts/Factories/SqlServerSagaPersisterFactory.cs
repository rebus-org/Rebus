using System.Data.SqlClient;
using Rebus.Persistence.SqlServer;
using Rebus.Timeout;
using log4net.Config;

namespace Rebus.Tests.Persistence.Timeouts.Factories
{
    public class SqlServerTimeoutStorageFactory : ITimeoutStorageFactory
    {
        static SqlServerTimeoutStorageFactory()
        {
            XmlConfigurator.Configure();
        }

        public IStoreTimeouts CreateStore()
        {
            DropTable("timeouts");
            return new SqlServerTimeoutStorage(ConnectionStrings.SqlServer, "timeouts").EnsureTableIsCreated();
        }

        void DropTable(string tableName)
        {
            ExecuteCommand("drop table " + tableName);
        }

        public void Dispose()
        {
        }

        protected void DeleteRows(string tableName)
        {
            ExecuteCommand("delete from " + tableName);
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