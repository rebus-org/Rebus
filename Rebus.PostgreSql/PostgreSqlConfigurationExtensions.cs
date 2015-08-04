using Rebus.Config;
using Rebus.Subscriptions;

namespace Rebus.PostgreSql
{
    /// <summary>
    /// Configuration extensions for Postgres persistence
    /// </summary>
    public static class PostgreSqlConfigurationExtensions
    {
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