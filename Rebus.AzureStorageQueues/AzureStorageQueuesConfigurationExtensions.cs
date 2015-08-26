using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Timeouts;
using Rebus.Transport;

namespace Rebus.AzureStorageQueues
{
    /// <summary>
    /// Configuration extensions for the Aure Storage Queue transport
    /// </summary>
    public static class AzureStorageQueuesConfigurationExtensions
    {
        const string AsqTimeoutManagerText = "A disabled timeout manager was installed as part of the Azure Storage Queues configuration, becuase the transport has native support for deferred messages";

        /// <summary>
        /// Configures Rebus to use Azure Storage Queues to transport messages as a one-way client (i.e. will not be able to receive any messages)
        /// </summary>
        public static void UseAzureStorageQueuesAsOneWayClient(this StandardConfigurer<ITransport> configurer, string storageAccountConnectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);

            Register(configurer, null, storageAccount);

            OneWayClientBackdoor.ConfigureOneWayClient(configurer);
        }

        /// <summary>
        /// Configures Rebus to use Azure Storage Queues to transport messages
        /// </summary>
        public static void UseAzureStorageQueues(this StandardConfigurer<ITransport> configurer, string storageAccountConnectionString, string inputQueueAddress)
        {
            var storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);

            Register(configurer, inputQueueAddress, storageAccount);
        }

        /// <summary>
        /// Configures Rebus to use Azure Storage Queues to transport messages as a one-way client (i.e. will not be able to receive any messages)
        /// </summary>
        public static void UseAzureStorageQueuesAsOneWayClient(this StandardConfigurer<ITransport> configurer, string accountName, string keyValue, bool useHttps)
        {
            var storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, keyValue), useHttps);

            Register(configurer, null, storageAccount);

            OneWayClientBackdoor.ConfigureOneWayClient(configurer);
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
        /// Configures Rebus to use Azure Storage Queues to transport messages as a one-way client (i.e. will not be able to receive any messages)
        /// </summary>
        public static void UseAzureStorageQueuesAsOneWayClient(this StandardConfigurer<ITransport> configurer, CloudStorageAccount storageAccount)
        {
            Register(configurer, null, storageAccount);

            OneWayClientBackdoor.ConfigureOneWayClient(configurer);
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

            configurer.OtherService<ITimeoutManager>().Register(c => new DisabledTimeoutManager(), description: AsqTimeoutManagerText);

            configurer.OtherService<IPipeline>().Decorate(c =>
            {
                var pipeline = c.Get<IPipeline>();
                
                return new PipelineStepRemover(pipeline)
                    .RemoveIncomingStep(s => s.GetType() == typeof(HandleDeferredMessagesStep));
            });
        }
    }
}