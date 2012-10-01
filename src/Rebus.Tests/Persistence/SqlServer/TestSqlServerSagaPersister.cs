using NUnit.Framework;
using Rebus.Persistence.SqlServer;
using Shouldly;

namespace Rebus.Tests.Persistence.SqlServer
{
    [TestFixture, Category(TestCategories.MsSql)]
    public class TestSqlServerSagaPersister : SqlServerFixtureBase
    {
        SqlServerSagaPersister persister;
        const string SagaTableName = "testSagaTable";
        const string SagaIndexTableName = "testSagaIndexTable";

        protected override void DoSetUp()
        {
            // ensure the two tables are dropped
            try { ExecuteCommand("drop table " + SagaTableName); }
            catch { }
            try { ExecuteCommand("drop table " + SagaIndexTableName); }
            catch { }

            persister = new SqlServerSagaPersister(ConnectionStrings.SqlServer, SagaIndexTableName, SagaTableName);
        }

        [Test]
        public void CanCreateSagaTablesAutomatically()
        {
            // arrange

            // act
            persister.EnsureTablesAreCreated();

            // assert
            var existingTables = GetTableNames();
            existingTables.ShouldContain(SagaIndexTableName);
            existingTables.ShouldContain(SagaTableName);
        }

        [Test]
        public void DoesntDoAnythingIfTheTablesAreAlreadyThere()
        {
            // arrange
            ExecuteCommand("create table " + SagaTableName + "(id int not null)");
            ExecuteCommand("create table " + SagaIndexTableName + "(id int not null)");

            // act
            // assert
            persister.EnsureTablesAreCreated();
            persister.EnsureTablesAreCreated();
            persister.EnsureTablesAreCreated();
        }
    }
}