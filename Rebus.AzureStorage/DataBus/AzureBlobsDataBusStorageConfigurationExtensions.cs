using System;
using Microsoft.WindowsAzure.Storage;
using Rebus.Config;
using Rebus.DataBus;
using Rebus.Logging;

namespace Rebus.AzureStorage.DataBus
{
    /// <summary>
    /// Configuration extensions for Azure-based data bus storage
    /// </summary>
    public static class AzureBlobsDataBusStorageConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus' data bus to storage data in Azure blobs in the given storage account and container
        /// </summary>
        public static void StoreInBlobStorage(this StandardConfigurer<IDataBusStorage> configurer, CloudStorageAccount storageAccount, string containerName)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));
            if (containerName == null) throw new ArgumentNullException(nameof(containerName));
            if (storageAccount == null) throw new ArgumentNullException(nameof(storageAccount));

            Configure(configurer, containerName, storageAccount);
        }

        /// <summary>
        /// Configures Rebus' data bus to storage data in Azure blobs in the given storage account and container
        /// </summary>
        public static void StoreInBlobStorage(this StandardConfigurer<IDataBusStorage> configurer, string storageAccountConnectionString, string containerName)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));
            if (containerName == null) throw new ArgumentNullException(nameof(containerName));
            if (storageAccountConnectionString == null) throw new ArgumentNullException(nameof(storageAccountConnectionString));

            var cloudStorageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);

            Configure(configurer, containerName, cloudStorageAccount);
        }

        static void Configure(StandardConfigurer<IDataBusStorage> configurer, string containerName, CloudStorageAccount cloudStorageAccount)
        {
            configurer.Register(c => new AzureBlobsDataBusStorage(cloudStorageAccount,containerName, c.Get<IRebusLoggerFactory>()));
        }
    }
}