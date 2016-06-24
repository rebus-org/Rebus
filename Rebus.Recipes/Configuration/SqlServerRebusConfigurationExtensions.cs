using Rebus.Config;
using Rebus.DataBus;
using Rebus.DataBus.SqlServer;
using Rebus.Logging;
using Rebus.Persistence.SqlServer;
using Rebus.Transport.SqlServer;

namespace Rebus.Recipes.Configuration
{
    /// <summary>
    /// Configures rebus to use sql server to store everything.
    /// </summary>
    public static class SqlServerRebusConfigurationExtensions
    {
        /// <summary>
        /// Configures rebus to use sql server to store everything.
        /// </summary>
        /// <param name="config">the rebus configuration</param>
        /// <param name="connectionStringOrName">the connectionstring or name of the connectionstring to use for sql server</param>
        /// <param name="queueName">the name of th rebus queue for this bus instance</param>
        /// <param name="queueTableName">the name of the queue table in the database</param>
        /// <param name="sagaDataTableName">the name of the saga data table in the database</param>
        /// <param name="sagaIndexTableName">the name of the saga index table in the database</param>
        /// <param name="subscriptionsTableName">the name of the subscriptions table in the database</param>
        /// <param name="timeoutTableName">the name of the timeouts table in the database</param>
        /// <param name="dataBusTableName">the name of the databus table in the database</param>
        /// <param name="enableDataBus">should rebus use a separate storage for large messages?</param>
        /// <param name="automaticallyCreateTables">should rebus auto-create tables?</param>
        /// <param name="isCenteralizedSubscriptions">is it safe to treat this as a centeralized location for subscriptions?</param>
        /// <returns>the rebus configuration</returns>
        public static RebusConfigurer UseSqlServer(this RebusConfigurer config, string connectionStringOrName, string queueName, 
            string queueTableName = "RebusMessageQueue",
            string sagaDataTableName = "RebusSaga",
            string sagaIndexTableName = "RebusSagaIndex",
            string subscriptionsTableName = "RebusSubscriptions",
            string timeoutTableName = "RebusTimeouts",
            string dataBusTableName = "RebusDataBus",
            bool enableDataBus = true,
            bool automaticallyCreateTables = true,
            bool isCenteralizedSubscriptions = false
            )
        {
            return config.Transport(t=>t.UseSqlServer(connectionStringOrName, queueTableName, queueName))
                .Sagas(s=>s.StoreInSqlServer(connectionStringOrName, sagaDataTableName, sagaIndexTableName, automaticallyCreateTables))
                .Subscriptions(s=>s.StoreInSqlServer(connectionStringOrName, subscriptionsTableName, isCenteralizedSubscriptions, automaticallyCreateTables))
                .Timeouts(t => t.StoreInSqlServer(connectionStringOrName, timeoutTableName, automaticallyCreateTables))
                .Options(o =>
                {
                    if (enableDataBus)
                    {

                        o.EnableDataBus().Register(d =>
                        {
                            var rebusLoggerFactory = d.Get<IRebusLoggerFactory>();
                            var connectionProvider = new DbConnectionProvider(connectionStringOrName, rebusLoggerFactory);
                            return new SqlServerDataBusStorage(connectionProvider, dataBusTableName, automaticallyCreateTables, rebusLoggerFactory);
                        });
                    }
                });
        }

        /// <summary>
        /// Configures rebus to use sql server to store everything in one-way client mode.
        /// </summary>
        /// <param name="config">the rebus configuration</param>
        /// <param name="connectionStringOrName">the connectionstring or name of the connectionstring to use for sql server</param>
        
        /// <param name="queueTableName">the name of the queue table in the database</param>
        /// <param name="sagaDataTableName">the name of the saga data table in the database</param>
        /// <param name="sagaIndexTableName">the name of the saga index table in the database</param>
        /// <param name="subscriptionsTableName">the name of the subscriptions table in the database</param>
        /// <param name="timeoutTableName">the name of the timeouts table in the database</param>
        /// <param name="dataBusTableName">the name of the databus table in the database</param>
        /// <param name="enableDataBus">should rebus use a separate storage for large messages?</param>
        /// <param name="automaticallyCreateTables">should rebus auto-create tables?</param>
        /// <param name="isCenteralizedSubscriptions">is it safe to treat this as a centeralized location for subscriptions?</param>
        /// <returns>the rebus configuration</returns>
        public static RebusConfigurer UseSqlServerAsOneWayClient(this RebusConfigurer config, string connectionStringOrName,
            string queueTableName = "RebusMessageQueue",
            string sagaDataTableName = "RebusSaga",
            string sagaIndexTableName = "RebusSagaIndex",
            string subscriptionsTableName = "RebusSubscriptions",
            string timeoutTableName = "RebusTimeouts",
            string dataBusTableName = "RebusDataBus",
            bool enableDataBus = true,
            bool automaticallyCreateTables = true,
            bool isCenteralizedSubscriptions = false
            )
        {
            return config.Transport(t => t.UseSqlServerAsOneWayClient(connectionStringOrName, queueTableName))
                .Sagas(s => s.StoreInSqlServer(connectionStringOrName, sagaDataTableName, sagaIndexTableName, automaticallyCreateTables))
                .Subscriptions(s => s.StoreInSqlServer(connectionStringOrName, subscriptionsTableName, isCenteralizedSubscriptions, automaticallyCreateTables))
                .Timeouts(t => t.StoreInSqlServer(connectionStringOrName, timeoutTableName, automaticallyCreateTables))
                .Options(o =>
                {
                    if (enableDataBus)
                    {

                        o.EnableDataBus().Register(d =>
                        {
                            var rebusLoggerFactory = d.Get<IRebusLoggerFactory>();
                            var connectionProvider = new DbConnectionProvider(connectionStringOrName, rebusLoggerFactory);
                            return new SqlServerDataBusStorage(connectionProvider, dataBusTableName, automaticallyCreateTables, rebusLoggerFactory);
                        });
                    }
                });
        }
    }
}