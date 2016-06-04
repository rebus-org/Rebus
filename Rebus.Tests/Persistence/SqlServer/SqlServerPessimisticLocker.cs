using Rebus.Logging;
using Rebus.Persistence.SqlServer;
using Rebus.Sagas.Locking;
using Rebus.Tests.Contracts.Locks;

namespace Rebus.Tests.Persistence.SqlServer
{
    public class SqlServerPessimisticLockerFactory : IPessimisticLockerFactory
    {
        public IPessimisticLocker Create()
        {
            var consoleLoggerFactory = new ConsoleLoggerFactory(false);
            var connectionProvider = new DbConnectionProvider(SqlTestHelper.ConnectionString, consoleLoggerFactory);
            return new SqlServerPessimisticLocker(connectionProvider);
        }
    }
}