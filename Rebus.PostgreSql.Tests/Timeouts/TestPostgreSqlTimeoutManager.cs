using NUnit.Framework;
using Rebus.Logging;
using Rebus.PostgreSql.Timeouts;
using Rebus.Tests.Contracts.Timeouts;
using Rebus.Timeouts;

namespace Rebus.PostgreSql.Tests.Timeouts
{
    [TestFixture, Category(TestCategory.Postgres)]
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
            var postgreSqlTimeoutManager = new PostgreSqlTimeoutManager(PostgreSqlTestHelper.ConnectionHelper, "timeouts", new ConsoleLoggerFactory(false));
            postgreSqlTimeoutManager.EnsureTableIsCreated();
            return postgreSqlTimeoutManager;
        }

        public void Cleanup()
        {
            PostgreSqlTestHelper.DropTable("timeouts");
        }
    }

}