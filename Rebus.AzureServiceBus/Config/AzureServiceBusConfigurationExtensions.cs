using System.Configuration;
using Rebus.AzureServiceBus;
using Rebus.Transport;

namespace Rebus.Config
{
    public static class AzureServiceBusConfigurationExtensions
    {
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