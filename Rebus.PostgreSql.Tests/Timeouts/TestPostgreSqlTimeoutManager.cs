using NUnit.Framework;
using Rebus.PostgreSql.Timeouts;
using Rebus.Tests.Contracts.Timeouts;
using Rebus.Timeouts;

namespace Rebus.PostgreSql.Tests.Timeouts
{
    [TestFixture]
    public class TestPostgreSqlTimeoutManager : BasicStoreAndRetrieveOperations<PostgreSqlTimeoutManagerFactory>
    {
    }

    public class PostgreSqlTimeoutManagerFactory : ITimeoutManagerFactory
    {
        public PostgreSqlTimeoutManagerFactory()
        {
            PostgreSqlTestHelper.DropTable("timeouts");
        }

        public ITimeoutManager Create()
        {
            var postgreSqlTimeoutManager = new PostgreSqlTimeoutManager(PostgreSqlTestHelper.ConnectionHelper, "timeouts");
            postgreSqlTimeoutManager.EnsureTableIsCreated();
            return postgreSqlTimeoutManager;
        }

        public void Cleanup()
        {
            PostgreSqlTestHelper.DropTable("timeouts");
        }
    }

}