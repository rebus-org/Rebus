using Rebus.Persistence.SqlServer;

namespace Rebus.Configuration
{
    /// <summary>
    /// Fluent configurer that allows for further configuration of the installed <see cref="SqlServerTimeoutStorage"/>
    /// </summary>
    public class SqlServerTimeoutStorageFluentConfigurer
    {
        readonly SqlServerTimeoutStorage sqlServerTimeoutStorage;

        internal SqlServerTimeoutStorageFluentConfigurer(SqlServerTimeoutStorage sqlServerTimeoutStorage)
        {
            this.sqlServerTimeoutStorage = sqlServerTimeoutStorage;
        }

        /// <summary>
        /// Checks to see if a table exists with the configured name - if that is not the case, it will be created.
        /// If a table already exists, nothing happens.
        /// </summary>
        public void EnsureTableIsCreated()
        {
            sqlServerTimeoutStorage.EnsureTableIsCreated();
        }
    }
}