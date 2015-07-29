using Rebus.Configuration;

namespace Rebus.PostgreSql
{
    /// <summary>
    /// Configuration extensions to allow for fluently configuring Rebus with PostgreSql.
    /// </summary>
    public static class PostgreSqlExtensions
    {
        /// <summary>
        /// Configures Rebus to store subscriptions in PostgreSQL.
        /// </summary>
        public static PostgreSqlSubscriptionStorageFluentConfigurer StoreInPostgreSql(this RebusSubscriptionsConfigurer configurer, string connectionString, string subscriptionsTableName)
        {
            var storage = new PostgreSqlSubscriptionStorage(connectionString, subscriptionsTableName);

            configurer.Use(storage);

            return new PostgreSqlSubscriptionStorageFluentConfigurer(storage);
        }

        /// <summary>
        /// Configures Rebus to store sagas in PostgreSQL.
        /// </summary>
        public static PostgreSqlSagaPersisterFluentConfigurer StoreInPostgreSql(this RebusSagasConfigurer configurer, string connectionString, string sagaTable, string sagaIndexTable)
        {
            var persister = new PostgreSqlSagaPersister(connectionString, sagaTable, sagaIndexTable);

            configurer.Use(persister);

            return new PostgreSqlSagaPersisterFluentConfigurer(persister);
        }

        /// <summary>
        /// Configures Rebus to store timeouts in PostgreSQL.
        /// </summary>
        public static PostgreSqlTimeoutStorageFluentConfigurer StoreInPostgreSql(this RebusTimeoutsConfigurer configurer, string connectionString, string timeoutsTableName)
        {
            var storage = new PostgreSqlTimeoutStorage(connectionString, timeoutsTableName);

            configurer.Use(storage);

            return new PostgreSqlTimeoutStorageFluentConfigurer(storage);
        }

        /// <summary>
        /// Fluent configurer that allows for configuring the underlying <see cref="PostgreSqlSubscriptionStorageFluentConfigurer"/>
        /// </summary>
        public class PostgreSqlSubscriptionStorageFluentConfigurer
        {
            readonly PostgreSqlSubscriptionStorage postgreSqlSubscriptionStorage;

            public PostgreSqlSubscriptionStorageFluentConfigurer(PostgreSqlSubscriptionStorage postgreSqlSubscriptionStorage)
            {
                this.postgreSqlSubscriptionStorage = postgreSqlSubscriptionStorage;
            }

            /// <summary>
            /// Checks to see if the underlying SQL tables are created - if none of them exist,
            /// they will automatically be created
            /// </summary>
            public PostgreSqlSubscriptionStorageFluentConfigurer EnsureTableIsCreated()
            {
                postgreSqlSubscriptionStorage.EnsureTableIsCreated();

                return this;
            }
        }

        /// <summary>
        /// Fluent configurer that allows for configuring the underlying <see cref="PostgreSqlSagaPersister"/>
        /// </summary>
        public class PostgreSqlSagaPersisterFluentConfigurer
        {
            readonly PostgreSqlSagaPersister postgreSqlSagaPersister;

            public PostgreSqlSagaPersisterFluentConfigurer(PostgreSqlSagaPersister postgreSqlSagaPersister)
            {
                this.postgreSqlSagaPersister = postgreSqlSagaPersister;
            }

            /// <summary>
            /// Checks to see if the underlying SQL tables are created - if none of them exist,
            /// they will automatically be created
            /// </summary>
            public PostgreSqlSagaPersisterFluentConfigurer EnsureTablesAreCreated()
            {
                postgreSqlSagaPersister.EnsureTablesAreCreated();

                return this;
            }

            /// <summary>
            /// Configures the persister to ignore null-valued correlation properties and not add them to the saga index.
            /// </summary>
            public PostgreSqlSagaPersisterFluentConfigurer DoNotIndexNullProperties()
            {
                postgreSqlSagaPersister.DoNotIndexNullProperties();

                return this;
            }
        }

        /// <summary>
        /// Fluent configurer that allows for configuring the underlying <see cref="PostgreSqlTimeoutStorageFluentConfigurer"/>
        /// </summary>
        public class PostgreSqlTimeoutStorageFluentConfigurer
        {
            readonly PostgreSqlTimeoutStorage postgreSqlTimeoutStorage;

            public PostgreSqlTimeoutStorageFluentConfigurer(PostgreSqlTimeoutStorage postgreSqlTimeoutStorage)
            {
                this.postgreSqlTimeoutStorage = postgreSqlTimeoutStorage;
            }

            /// <summary>
            /// Checks to see if the underlying SQL tables are created - if none of them exist,
            /// they will automatically be created
            /// </summary>
            public PostgreSqlTimeoutStorageFluentConfigurer EnsureTableIsCreated()
            {
                postgreSqlTimeoutStorage.EnsureTableIsCreated();

                return this;
            }
        }
    }
}