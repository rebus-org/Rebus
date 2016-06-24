using System;
using Microsoft.WindowsAzure.Storage;
using Rebus.Auditing.Sagas;
using Rebus.AzureStorage.DataBus;
using Rebus.Config;
using Rebus.DataBus;

namespace Rebus.AzureStorage.Config
{
    /// <summary>
    /// Configuration extensions for general Azure configuration
    /// </summary>
    public static class AzureConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use a full Azure storage-based setup with storage queues as transport, table storage
        /// for subscriptions, and a combined table storage- and blob-based saga storage.
        /// Optionally enables the data bus (using blob storage) and saga snapshots (using blob storage too)
        /// </summary>
        public static RebusConfigurer UseAzure(this RebusConfigurer configurer, string storageAccountConnectionStringOrName, string inputQueueAddress)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));
            if (storageAccountConnectionStringOrName == null) throw new ArgumentNullException(nameof(storageAccountConnectionStringOrName));
            if (inputQueueAddress == null) throw new ArgumentNullException(nameof(inputQueueAddress));

            var storageAccount = AzureConfigurationHelper.GetStorageAccount(storageAccountConnectionStringOrName);

            return Configure(configurer, storageAccount, inputQueueAddress);
        }

        static RebusConfigurer Configure(this RebusConfigurer configurer, CloudStorageAccount storageAccount, string inputQueueAddress,
            string subscriptionTableName = "RebusSubscriptions",
            string sagaIndexTableName = "RebusSagaIndex",
            string sagaContainerName = "RebusSagaStorage",
            string dataBusContainerName = "RebusDataBusData",
            bool isCentralizedSubscriptions = false,
            bool enableDataBus = true,
            bool enableSagaSnapshots = true)
        {
            return configurer
                .Transport(t => t.UseAzureStorageQueues(storageAccount, inputQueueAddress))
                .Subscriptions(t => t.StoreInTableStorage(storageAccount, subscriptionTableName, isCentralizedSubscriptions))
                .Sagas(t => t.StoreInAzureStorage(storageAccount, sagaIndexTableName, sagaContainerName))
                .Options(o =>
                {
                    if (enableDataBus)
                    {
                        o.EnableDataBus().StoreInBlobStorage(storageAccount, dataBusContainerName);
                    }
                    if (enableSagaSnapshots)
                    {
                        o.EnableSagaAuditing().StoreInBlobStorage(storageAccount, sagaContainerName);
                    }
                });
        }
    }
}