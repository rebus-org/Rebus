using System;
using System.Data.SqlClient;
using Rebus.Persistence.InMemory;
using Rebus.Persistence.SqlServer;
using Rebus.Transports.Sql;

namespace Rebus.Configuration
{
    /// <summary>
    /// Configurer that allows for configuring how sagas are made persistent
    /// </summary>
    public class RebusSagasConfigurer : BaseConfigurer
    {
        internal RebusSagasConfigurer(ConfigurationBackbone backbone)
            : base(backbone)
        {
        }

        /// <summary>
        /// Uses the specified implementation of <see cref="IStoreSagaData"/> to persist saga data
        /// </summary>
        public void Use(IStoreSagaData storeSagaData)
        {
            Backbone.StoreSagaData = storeSagaData;
        }

        /// <summary>
        /// Configures Rebus to store sagas in SQL Server. Use this overload when your saga doesn't perform
        /// any additional work in the same SQL Server.
        /// </summary>
        public SqlServerSagaPersisterFluentConfigurer StoreInSqlServer(string connectionstring, string sagaTable, string sagaIndexTable)
        {
            var persister = new SqlServerSagaPersister(connectionstring, sagaIndexTable, sagaTable);
            Use(persister);
            return new SqlServerSagaPersisterFluentConfigurer(persister);
        }

        /// <summary>
        /// Configures Rebus to store sagas in SQL Server. Use this overload to have the persister use the
        /// same <see cref="SqlConnection"/> as you're using, thus enlisting in whatever transactional
        /// behavior you might be using.
        /// </summary>
        public SqlServerSagaPersisterFluentConfigurer StoreInSqlServer(Func<ConnectionHolder> connectionFactoryMethod, string sagaTable, string sagaIndexTable)
        {
            var persister = new SqlServerSagaPersister(connectionFactoryMethod, sagaIndexTable, sagaTable);
            Use(persister);
            return new SqlServerSagaPersisterFluentConfigurer(persister);
        }

        /// <summary>
        /// Configures Rebus to store sagas in memory. This should only be used for very short-lived sagas
        /// that you can afford to lose on each restart/server crash etc. It's also cool for debugging and
        /// running stuff locally.
        /// </summary>
        public void StoreInMemory()
        {
            Use(new InMemorySagaPersister());
        }
    }
}