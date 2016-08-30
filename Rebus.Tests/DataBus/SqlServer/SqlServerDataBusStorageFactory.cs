using Rebus.DataBus;
using Rebus.DataBus.SqlServer;
using Rebus.Logging;
using Rebus.Persistence.SqlServer;
using Rebus.Tests.Contracts.DataBus;

namespace Rebus.Tests.DataBus.SqlServer
{
    public class SqlServerDataBusStorageFactory : IDataBusStorageFactory
    {
        public SqlServerDataBusStorageFactory()
        {
            SqlTestHelper.DropTable("databus");
        }

        public IDataBusStorage Create()
        {
            var consoleLoggerFactory = new ConsoleLoggerFactory(false);
            var connectionProvider = new DbConnectionProvider(SqlTestHelper.ConnectionString, consoleLoggerFactory);
            var sqlServerDataBusStorage = new SqlServerDataBusStorage(connectionProvider, "databus", true, consoleLoggerFactory);
            sqlServerDataBusStorage.Initialize();
            return sqlServerDataBusStorage;
        }

        public void CleanUp()
        {
            SqlTestHelper.DropTable("databus");
        }
    }
}