using NUnit.Framework;
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
            var timeoutManager = new SqlServerTimeoutManager(new DbConnectionProvider(SqlTestHelper.ConnectionString), TableName);

            timeoutManager.EnsureTableIsCreated();

            return timeoutManager;
        }

        public void Cleanup()
        {
            SqlTestHelper.DropTable(TableName);
        }
    }
}