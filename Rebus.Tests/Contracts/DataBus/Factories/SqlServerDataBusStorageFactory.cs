using Rebus.DataBus;
using Rebus.DataBus.SqlServer;
using Rebus.Logging;
using Rebus.Persistence.SqlServer;

namespace Rebus.Tests.Contracts.DataBus.Factories
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
    }
}