using System.Configuration;
using Rebus.AzureServiceBus;
using Rebus.Transport;

// ReSharper disable once CheckNamespace
namespace Rebus.Config
{
    /// <summary>
    /// Configuration extensions for the Azure Service Bus transport
    /// </summary>
    public static class AzureServiceBusConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use Azure Service Bus queues to transport messages, connecting to the service bus instance pointed to by the connection string
        /// (or the connection string with the specified name from the current app.config)
        /// </summary>
        public static AzureServiceBusTransportSettings UseAzureServiceBus(this StandardConfigurer<ITransport> configurer, string connectionStringNameOrConnectionString, string inputQueueAddress)
        {
            var connectionString = GetConnectionString(connectionStringNameOrConnectionString);
            var settingsBuilder = new AzureServiceBusTransportSettings();

            configurer.Register(c =>
            {
                var transport = new AzureServiceBusTransport(connectionString, inputQueueAddress);

                if (settingsBuilder.PrefetchingEnabled)
                {
                    transport.PrefetchMessages(settingsBuilder.NumberOfMessagesToPrefetch);
                }

                if (settingsBuilder.AutomaticPeekLockRenewalEnabled)
                {
                    transport.AutomaticallyRenewPeekLock();
                }

                return transport;
            });

            return settingsBuilder;
        }

        static string GetConnectionString(string connectionStringNameOrConnectionString)
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionStringNameOrConnectionString];

            if (connectionStringSettings == null)
            {
                return connectionStringNameOrConnectionString;
            }

            return connectionStringNameOrConnectionString;
        }
    }
}