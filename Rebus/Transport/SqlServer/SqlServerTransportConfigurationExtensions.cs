using Rebus.Config;
using Rebus.Logging;
using Rebus.Persistence.SqlServer;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Timeouts;

namespace Rebus.Transport.SqlServer
{
    /// <summary>
    /// Configuration extensions for the SQL transport
    /// </summary>
    public static class SqlServerTransportConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use SQL Server to transport messages as a one-way client (i.e. will not be able to receive any messages).
        /// The table specified by <paramref name="tableName"/> will be used to store messages.
        /// The message table will automatically be created if it does not exist.
        /// </summary>
        public static void UseSqlServerAsOneWayClient(this StandardConfigurer<ITransport> configurer, string connectionStringOrConnectionStringName, string tableName)
        {
            Configure(configurer, connectionStringOrConnectionStringName, tableName, null);

            OneWayClientBackdoor.ConfigureOneWayClient(configurer);
        }

        /// <summary>
        /// Configures Rebus to use SQL Server as its transport. The table specified by <paramref name="tableName"/> will be used to
        /// store messages, and the "queue" specified by <paramref name="inputQueueName"/> will be used when querying for messages.
        /// The message table will automatically be created if it does not exist.
        /// </summary>
        public static void UseSqlServer(this StandardConfigurer<ITransport> configurer, string connectionStringOrConnectionStringName, string tableName, string inputQueueName)
        {
            Configure(configurer, connectionStringOrConnectionStringName, tableName, inputQueueName);
        }

        static void Configure(StandardConfigurer<ITransport> configurer, string connectionString, string tableName, string inputQueueName)
        {
            configurer.Register(context =>
            {
                var rebusLoggerFactory = context.Get<IRebusLoggerFactory>();
                var connectionProvider = new DbConnectionProvider(connectionString, rebusLoggerFactory);
                var transport = new SqlServerTransport(connectionProvider, tableName, inputQueueName, rebusLoggerFactory);
                transport.EnsureTableIsCreated();
                return transport;
            });

            configurer.OtherService<ITimeoutManager>().Register(c => new DisabledTimeoutManager());

            configurer.OtherService<IPipeline>().Decorate(c =>
            {
                var pipeline = c.Get<IPipeline>();

                return new PipelineStepRemover(pipeline)
                    .RemoveIncomingStep(s => s.GetType() == typeof (HandleDeferredMessagesStep));
            });
        }
    }
}