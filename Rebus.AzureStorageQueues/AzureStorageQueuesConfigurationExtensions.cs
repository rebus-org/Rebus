using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Rebus.Config;
using Rebus.Transport;

namespace Rebus.AzureStorageQueues
{
    /// <summary>
    /// Configuration extensions for the Aure Storage Queue transport
    /// </summary>
    public static class AzureStorageQueuesConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use Azure Storage Queues to transport messages
        /// </summary>
        public static void UseAzureStorageQueues(this StandardConfigurer<ITransport> configurer, string storageAccountConnectionString, string inputQueueAddress)
        {
            var storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);

            Register(configurer, inputQueueAddress, storageAccount);
        }

        /// <summary>
        /// Configures Rebus to use Azure Storage Queues to transport messages
        /// </summary>
        public static void UseAzureStorageQueues(this StandardConfigurer<ITransport> configurer, string accountName, string keyValue, bool useHttps, string inputQueueAddress)
        {
            var storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, keyValue), useHttps);

            Register(configurer, inputQueueAddress, storageAccount);
        }

        /// <summary>
        /// Configures Rebus to use Azure Storage Queues to transport messages
        /// </summary>
        public static void UseAzureStorageQueues(this StandardConfigurer<ITransport> configurer, CloudStorageAccount storageAccount, string inputQueueAddress)
        {
            Register(configurer, inputQueueAddress, storageAccount);
        }

        static void Register(StandardConfigurer<ITransport> configurer, string inputQueueAddress, CloudStorageAccount storageAccount)
        {
            configurer.Register(c => new AzureStorageQueuesTransport(storageAccount, inputQueueAddress));
        }
    }
}