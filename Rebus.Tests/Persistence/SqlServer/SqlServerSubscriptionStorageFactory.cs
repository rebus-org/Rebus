using Rebus.Logging;
using Rebus.Persistence.SqlServer;
using Rebus.Subscriptions;
using Rebus.Tests.Contracts.Subscriptions;

namespace Rebus.Tests.Persistence.SqlServer
{
    public class SqlServerSubscriptionStorageFactory : ISubscriptionStorageFactory
    {
        const string TableName = "RebusSubscriptions";
        
        public ISubscriptionStorage Create()
        {
            var consoleLoggerFactory = new ConsoleLoggerFactory(true);
            var connectionProvider = new DbConnectionProvider(SqlTestHelper.ConnectionString, consoleLoggerFactory);
            var storage = new SqlServerSubscriptionStorage(connectionProvider, TableName, true, consoleLoggerFactory);

            storage.EnsureTableIsCreated();
            
            return storage;
        }

        public void Cleanup()
        {
            SqlTestHelper.DropTable(TableName);
        }
    }
}