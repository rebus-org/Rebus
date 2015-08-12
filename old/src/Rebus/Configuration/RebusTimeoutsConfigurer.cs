using Rebus.Messages;
using Rebus.Persistence.InMemory;
using Rebus.Persistence.SqlServer;
using Rebus.Timeout;

namespace Rebus.Configuration
{
    /// <summary>
    /// Configurer to configure how Rebus handles messages deferred to the future. Will decide which
    /// implementation of <see cref="IStoreTimeouts"/> that goes into Rebus, which ultimately decides
    /// whether <see cref="TimeoutRequest"/>s are sent locally or to an external timeout manager
    /// </summary>
    public class RebusTimeoutsConfigurer : BaseConfigurer
    {
        internal RebusTimeoutsConfigurer(ConfigurationBackbone backbone) : base(backbone)
        {
        }

        /// <summary>
        /// Configures Rebus to store timeouts externally
        /// </summary>
        public void UseExternalTimeoutManager()
        {
            Backbone.StoreTimeouts = null;
        }

        /// <summary>
        /// Configures Rebus to store timeouts in SQL server
        /// </summary>
        public SqlServerTimeoutStorageFluentConfigurer StoreInSqlServer(string connectionString, string timeoutsTableName)
        {
            var timeoutStorage = new SqlServerTimeoutStorage(connectionString, timeoutsTableName);
            Use(timeoutStorage);
            return new SqlServerTimeoutStorageFluentConfigurer(timeoutStorage);
        }

        /// <summary>
        /// Configures Rebus to store timeouts internally in memory. This option should probably not be used for production scenarios, because
        /// timeouts will not survive a restart.
        /// </summary>
        public void StoreInMemory()
        {
            Use(new InMemoryTimeoutStorage());
        }

        /// <summary>
        /// Installs the given implementation of <see cref="IStoreTimeouts"/>
        /// </summary>
        public void Use(IStoreTimeouts storeTimeouts)
        {
            Backbone.StoreTimeouts = storeTimeouts;
        }
    }
}