using NUnit.Framework;
using Rebus.Persistence.SqlServer;
using Rebus.Sagas;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.Tests.Persistence.SqlServer
{
    [TestFixture]
    public class BasicLoadAndSaveAndFindOperations : BasicLoadAndSaveAndFindOperations<SqlServerSagaStorageFactory> { }

    [TestFixture]
    public class ConcurrencyHandling : ConcurrencyHandling<SqlServerSagaStorageFactory> { }

    public class SqlServerSagaStorageFactory : ISagaStorageFactory
    {
        const string IndexTableName = "RebusSagaData";
        const string DataTableName = "RebusSagaIndex";

        public ISagaStorage GetSagaStorage()
        {
            var storage = new SqlServerSagaStorage(new DbConnectionProvider(SqlTestHelper.ConnectionString), DataTableName, IndexTableName);

            storage.EnsureTablesAreCreated();

            return storage;
        }

        public void Cleanup()
        {
            SqlTestHelper.DropTable(IndexTableName);
            SqlTestHelper.DropTable(DataTableName);
        }
    }
}