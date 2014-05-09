using NUnit.Framework;
using Rebus.PostgreSql;
using Shouldly;

namespace Rebus.Tests.Persistence.PostgreSql
{
    [TestFixture, Category(TestCategories.PostgreSql)]
    public class TestPostgreSqlTimeoutStorage : PostgreSqlFixtureBase
    {
        PostgreSqlTimeoutStorage storage;
        const string TimeoutsTableName = "timeouts";

        [Test]
        public void CanCreateStorageTableAutomatically()
        {
            storage.EnsureTableIsCreated();

            var tableNames = GetTableNames();
            tableNames.ShouldContain(TimeoutsTableName);
        }

        [Test]
        public void DoesntDoAnythingIfTheTableAlreadyExists()
        {
            ExecuteCommand(@"CREATE TABLE """ + TimeoutsTableName + @""" (""id"" INT NOT NULL)");

            storage.EnsureTableIsCreated();
            storage.EnsureTableIsCreated();
            storage.EnsureTableIsCreated();
        }

        protected override void DoSetUp()
        {
            DropTable(TimeoutsTableName);

            storage = new PostgreSqlTimeoutStorage(ConnectionString, TimeoutsTableName);
        }
    }
}