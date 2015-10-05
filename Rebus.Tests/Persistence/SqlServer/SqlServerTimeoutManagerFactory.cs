using NUnit.Framework;
using Rebus.Logging;
using Rebus.Persistence.SqlServer;
using Rebus.Tests.Contracts.Timeouts;
using Rebus.Timeouts;

namespace Rebus.Tests.Persistence.SqlServer
{
    [TestFixture, Category(Categories.SqlServer)]
    public class BasicStoreAndRetrieveOperations : BasicStoreAndRetrieveOperations<SqlServerTimeoutManagerFactory>
    {
    }

    public class SqlServerTimeoutManagerFactory : ITimeoutManagerFactory
    {
        const string TableName = "RebusTimeouts";

        public ITimeoutManager Create()
        {
            var consoleLoggerFactory = new ConsoleLoggerFactory(true);
            var connectionProvider = new DbConnectionProvider(SqlTestHelper.ConnectionString, consoleLoggerFactory);
            var timeoutManager = new SqlServerTimeoutManager(connectionProvider, TableName, consoleLoggerFactory);

            timeoutManager.EnsureTableIsCreated();

            return timeoutManager;
        }

        public void Cleanup()
        {
            SqlTestHelper.DropTable(TableName);
        }
    }
}