using Rebus.Persistence.SqlServer;

namespace Rebus.Configuration
{
    /// <summary>
    /// Fluent configurer that allows for configuring the underlying <see cref="SqlServerSagaPersister"/>
    /// </summary>
    public class SqlServerSagaPersisterFluentConfigurer
    {
        readonly SqlServerSagaPersister persister;

        internal SqlServerSagaPersisterFluentConfigurer(SqlServerSagaPersister persister)
        {
            this.persister = persister;
        }

        /// <summary>
        /// Checks to see if the underlying SQL tables are created - if none of them exist,
        /// they will automatically be created
        /// </summary>
        public SqlServerSagaPersisterFluentConfigurer EnsureTablesAreCreated()
        {
            persister.EnsureTablesAreCreated();
            return this;
        }
    }
}