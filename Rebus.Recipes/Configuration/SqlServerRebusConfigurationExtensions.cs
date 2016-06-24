using Rebus.Config;
using Rebus.DataBus;
using Rebus.DataBus.SqlServer;
using Rebus.Logging;
using Rebus.Persistence.SqlServer;
using Rebus.Transport.SqlServer;

namespace Rebus.Recipes.Configuration
{
    public static class SqlServerRebusConfigurationExtensions
    {
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