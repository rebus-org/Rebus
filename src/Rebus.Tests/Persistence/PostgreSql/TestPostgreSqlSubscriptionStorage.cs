using NUnit.Framework;
using Rebus.PostgreSql;
using Shouldly;

namespace Rebus.Tests.Persistence.PostgreSql
{
    [TestFixture, Category(TestCategories.PostgreSql)]
    public class TestPostgreSqlSubscriptionStorage : PostgreSqlFixtureBase
    {
        PostgreSqlSubscriptionStorage storage;
        const string SubscriptionsTableName = "testSubscriptionsTable";

        [Test]
        public void CanCreateSagaTablesAutomatically()
        {
            storage.EnsureTableIsCreated();

            var existingTables = GetTableNames();
            existingTables.ShouldContain(SubscriptionsTableName);
        }

        [Test]
        public void DoesntDoAnythingIfTheTablesAreAlreadyThere()
        {
            ExecuteCommand(@"CREATE TABLE """ + SubscriptionsTableName + @""" (""id"" INT NOT NULL)");

            storage.EnsureTableIsCreated();
            storage.EnsureTableIsCreated();
            storage.EnsureTableIsCreated();
        }

        protected override void DoSetUp()
        {
            DropTable(SubscriptionsTableName);

            storage = new PostgreSqlSubscriptionStorage(ConnectionString, SubscriptionsTableName);
        }
    }
}