using Rebus.Config;
using Rebus.PostgreSql.Sagas;
using Rebus.PostgreSql.Subscriptions;
using Rebus.PostgreSql.Timeouts;
using Rebus.Sagas;
using Rebus.Subscriptions;
using Rebus.Timeouts;

namespace Rebus.PostgreSql
{
    /// <summary>
    /// Configuration extensions for Postgres persistence
    /// </summary>
    public static class PostgreSqlConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use SQL Server to store sagas, using the tables specified to store data and indexed properties respectively.
        /// </summary>
        public static void StoreInPostgres(this StandardConfigurer<ISagaStorage> configurer,
            string connectionString, string dataTableName, string indexTableName,
            bool automaticallyCreateTables = true)
        {
            configurer.Register(c =>
            {
                var sagaStorage = new PostgreSqlSagaStorage(new PostgresConnectionHelper(connectionString), dataTableName, indexTableName);

                if (automaticallyCreateTables)
                {
                    sagaStorage.EnsureTablesAreCreated();
                }

                return sagaStorage;
            });
        }

        /// <summary>
        /// Configures Rebus to use PostgreSQL to store timeouts.
        /// </summary>
        public static void StoreInPostgres(this StandardConfigurer<ITimeoutManager> configurer, string connectionString, string tableName, bool automaticallyCreateTables = true)
        {
            configurer.Register(c =>
            {
                var subscriptionStorage = new PostgreSqlTimeoutManager(new PostgresConnectionHelper(connectionString), tableName);

                if (automaticallyCreateTables)
                {
                    subscriptionStorage.EnsureTableIsCreated();
                }

                return subscriptionStorage;
            });
        }

        /// <summary>
        /// Configures Rebus to use PostgreSQL to store subscriptions. Use <see cref="isCentralized"/> = true to indicate whether it's OK to short-circuit
        /// subscribing and unsubscribing by manipulating the subscription directly from the subscriber or just let it default to false to preserve the
        /// default behavior.
        /// </summary>
        public static void StoreInPostgres(this StandardConfigurer<ISubscriptionStorage> configurer,
            string connectionString, string tableName, bool isCentralized = false, bool automaticallyCreateTables = true)
        {
            configurer.Register(c =>
            {
                var subscriptionStorage = new PostgreSqlSubscriptionStorage(
                    new PostgresConnectionHelper(connectionString), tableName, isCentralized);

                if (automaticallyCreateTables)
                {
                    subscriptionStorage.EnsureTableIsCreated();
                }

                return subscriptionStorage;
            });
        }

    }
}