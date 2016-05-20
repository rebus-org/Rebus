using System;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Persistence.SqlServer;

namespace Rebus.DataBus.SqlServer
{
    /// <summary>
    /// Configuration extensions for SQL Server data bus
    /// </summary>
    public static class SqlServerDataBusConfigurationExtensions
    {
        /// <summary>
        /// Configures the data bus to store data in a central SQL Server 
        /// </summary>
        public static void StoreInSqlServer(this StandardConfigurer<IDataBusStorage> configurer, string connectionStringOrConnectionStringName, string tableName, bool automaticallyCreateTables = true)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));
            if (connectionStringOrConnectionStringName == null) throw new ArgumentNullException(nameof(connectionStringOrConnectionStringName));
            if (tableName == null) throw new ArgumentNullException(nameof(tableName));

            configurer.Register(c =>
            {
                var loggerFactory = c.Get<IRebusLoggerFactory>();
                var connectionProvider = new DbConnectionProvider(connectionStringOrConnectionStringName, loggerFactory);
                return new SqlServerDataBusStorage(connectionProvider, tableName, automaticallyCreateTables, loggerFactory);
            });
        }
    }
}