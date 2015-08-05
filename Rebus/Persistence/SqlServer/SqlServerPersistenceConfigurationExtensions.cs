using Rebus.Config;
using Rebus.Sagas;
using Rebus.Subscriptions;
using Rebus.Timeouts;

namespace Rebus.Persistence.SqlServer
{
    /// <summary>
    /// Configuration extensions for configuring SQL persistence for sagas, subscriptions, and timeouts.
    /// </summary>
    public static class SqlServerPersistenceConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use SQL Server to store sagas, using the tables specified to store data and indexed properties respectively.
        /// </summary>
        public static void StoreInSqlServer(this StandardConfigurer<ISagaStorage> configurer,
            string connectionStringOrConnectionStringName, string dataTableName, string indexTableName,
            bool automaticallyCreateTables = true)
        {
            configurer.Register(c =>
            {
                var sagaStorage = new SqlServerSagaStorage(
                    new DbConnectionProvider(connectionStringOrConnectionStringName),
                    dataTableName, indexTableName);

                if (automaticallyCreateTables)
                {
                    sagaStorage.EnsureTablesAreCreated();
                }

                return sagaStorage;
            });
        }

        /// <summary>
        /// Configures Rebus to use SQL Server to store subscriptions. Use <paramref name="isCentralized"/> = true to indicate whether it's OK to short-circuit
        /// subscribing and unsubscribing by manipulating the subscription directly from the subscriber or just let it default to false to preserve the
        /// default behavior.
        /// </summary>
        public static void StoreInSqlServer(this StandardConfigurer<ISubscriptionStorage> configurer,
            string connectionStringOrConnectionStringName, string tableName, bool isCentralized = false, bool automaticallyCreateTables = true)
        {
            configurer.Register(c =>
            {
                var subscriptionStorage = new SqlServerSubscriptionStorage(
                    new DbConnectionProvider(connectionStringOrConnectionStringName),
                    tableName, isCentralized);

                if (automaticallyCreateTables)
                {
                    subscriptionStorage.EnsureTableIsCreated();
                }

                return subscriptionStorage;
            });
        }

        /// <summary>
        /// Configures Rebus to use SQL Server to store timeouts.
        /// </summary>
        public static void StoreInSqlServer(this StandardConfigurer<ITimeoutManager> configurer, string connectionStringOrConnectionStringName, string tableName, bool automaticallyCreateTables = true)
        {
            configurer.Register(c =>
            {
                var subscriptionStorage = new SqlServerTimeoutManager(new DbConnectionProvider(connectionStringOrConnectionStringName), tableName);

                if (automaticallyCreateTables)
                {
                    subscriptionStorage.EnsureTableIsCreated();
                }

                return subscriptionStorage;
            });
        }
    }
}