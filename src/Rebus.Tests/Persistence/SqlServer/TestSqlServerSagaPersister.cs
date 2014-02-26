using NUnit.Framework;
using Rebus.Persistence.SqlServer;
using Shouldly;

namespace Rebus.Tests.Persistence.SqlServer
{
    [TestFixture, Category(TestCategories.MsSql)]
    public class TestSqlServerSagaPersister : SqlServerFixtureBase
    {
        SqlServerSagaPersister persister;
        
        protected override void DoSetUp()
        {
            DropeSagaTables();
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