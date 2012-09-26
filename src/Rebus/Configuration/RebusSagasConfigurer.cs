using System;
using System.Data.SqlClient;
using Rebus.Persistence.InMemory;
using Rebus.Persistence.SqlServer;

namespace Rebus.Configuration
{
    public class RebusSagasConfigurer : BaseConfigurer
    {
        public RebusSagasConfigurer(ConfigurationBackbone backbone)
            : base(backbone)
        {
        }

        public void Use(IStoreSagaData storeSagaData)
        {
            Backbone.StoreSagaData = storeSagaData;
        }

        /// <summary>
        /// Configures Rebus to store sagas in SQL Server. Use this overload when your saga doesn't perform
        /// any additional work in the same SQL Server.
        /// </summary>
        public void StoreInSqlServer(string connectionstring, string sagaTable, string sagaIndexTable)
        {
            Use(new SqlServerSagaPersister(connectionstring, sagaIndexTable, sagaTable));
        }

        /// <summary>
        /// Configures Rebus to store sagas in SQL Server. Use this overload to have the persister use the
        /// same <see cref="SqlConnection"/> as you're using, thus enlisting in whatever transactional
        /// behavior you might be using.
        /// </summary>
        public void StoreInSqlServer(Func<SqlConnection> connectionFactoryMethod, string sagaTable, string sagaIndexTable)
        {
            Use(new SqlServerSagaPersister(connectionFactoryMethod, sagaIndexTable, sagaTable));
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