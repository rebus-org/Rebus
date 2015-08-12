using NUnit.Framework;
using Rebus.Persistence.SqlServer;
using Shouldly;

namespace Rebus.Tests.Persistence.SqlServer
{
    [TestFixture, Category(TestCategories.MsSql)]
    public class TestSqlServerSubscriptionStorage : SqlServerFixtureBase
    {
        SqlServerSubscriptionStorage storage;
        const string SubscriptionsTableName = "testSubscriptionsTable";

        protected override void DoSetUp()
        {
            // ensure the two tables are dropped
            try { ExecuteCommand("drop table " + SubscriptionsTableName); }
            catch { }

            storage = new SqlServerSubscriptionStorage(ConnectionStrings.SqlServer, SubscriptionsTableName);
        }

        [Test]
        public void CanCreateSagaTablesAutomatically()
        {
            // arrange

            // act
            storage.EnsureTableIsCreated();

            // assert
            var existingTables = GetTableNames();
            existingTables.ShouldContain(SubscriptionsTableName);
        }

        [Test]
        public void DoesntDoAnythingIfTheTablesAreAlreadyThere()
        {
            // arrange
            ExecuteCommand("create table " + SubscriptionsTableName + "(id int not null)");

            // act
            // assert
            storage.EnsureTableIsCreated();
            storage.EnsureTableIsCreated();
            storage.EnsureTableIsCreated();
        }
    }
}