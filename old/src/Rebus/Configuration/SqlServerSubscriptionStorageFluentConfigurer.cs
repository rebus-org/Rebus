using Rebus.Persistence.SqlServer;

namespace Rebus.Configuration
{
    /// <summary>
    /// Fluent configurer that allows for configuring the underlying <see cref="SqlServerSubscriptionStorage"/>
    /// </summary>
    public class SqlServerSubscriptionStorageFluentConfigurer
    {
        readonly SqlServerSubscriptionStorage persister;

        internal SqlServerSubscriptionStorageFluentConfigurer(SqlServerSubscriptionStorage persister)
        {
            this.persister = persister;
        }

        /// <summary>
        /// Checks to see if the database contains the configured subscriptions table, and if that is not
        /// the case it will be created
        /// </summary>
        public SqlServerSubscriptionStorageFluentConfigurer EnsureTableIsCreated()
        {
            persister.EnsureTableIsCreated();
            return this;
        }
    }
}