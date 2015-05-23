using System.Configuration;
using Rebus.AzureTableStorage;
using Rebus.Transport;

// ReSharper disable once CheckNamespace
namespace Rebus.Config
{
    /// <summary>
    /// Configuration extensions for the Azure Service Bus transport
    /// </summary>
    public static class AzureTableStorageConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use Azure Tablestorage queues to transport messages, connecting to the tablestorage pointed to by the connection string
        /// (or the connection string with the specified name from the current app.config)
        /// Optimal for queues under 5000 messages at a time
        /// </summary>
        public static void UseAzureTableStorage(this StandardConfigurer<ITransport> configurer, string connectionStringNameOrConnectionString, string inputQueueAddress)
        {
            var connectionString = GetConnectionString(connectionStringNameOrConnectionString);
            configurer.Register(c => new AzureTableStorageTransport(connectionString,inputQueueAddress));
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