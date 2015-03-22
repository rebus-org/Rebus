using Rebus.Config;
using Rebus.Persistence.SqlServer;

namespace Rebus.Transport.SqlServer
{
    public static class SqlServerTransportConfigurationExtensions
    {
        public static void UseSqlServer(this StandardConfigurer<ITransport> configurer, string connectionStringOrConnectionStringName, string tableName, string inputQueueName)
        {
            configurer.Register(context =>
            {
                var connectionProvider = new DbConnectionProvider(connectionStringOrConnectionStringName);
                var transport = new SqlServerTransport(connectionProvider, tableName, inputQueueName);
                transport.EnsureTableIsCreated();
                return transport;
            });
        }
    }
}