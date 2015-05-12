using System.Configuration;
using Rebus.AzureServiceBus;
using Rebus.Transport;

namespace Rebus.Config
{
    /// <summary>
    /// Configuration extensions for the Azure Service Bus transport
    /// </summary>
    public static class AzureServiceBusConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use Azure Service Bus queues to transport messages
        /// </summary>
        public static void UseAzureServiceBus(this StandardConfigurer<ITransport> configurer, string connectionStringNameOrConnectionString, string inputQueueAddress)
        {
            var connectionString = GetConnectionString(connectionStringNameOrConnectionString);

            configurer.Register(c => new AzureServiceBusTransport(connectionString, inputQueueAddress));
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