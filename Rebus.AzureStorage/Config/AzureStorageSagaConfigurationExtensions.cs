using System;
using Microsoft.WindowsAzure.Storage;
using Rebus.Auditing.Sagas;
using Rebus.AzureStorage.Sagas;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Sagas;

namespace Rebus.AzureStorage.Config
{
    /// <summary>
    /// Configuration extensions for Azure storage
    /// </summary>
    public static class AzureStorageSagaConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use a combination of blob and table storage to store sagas
        /// </summary>
        public static void StoreInAzureStorage(this StandardConfigurer<ISagaStorage> configurer,
            string storageAccountConnectionStringOrName,
            string tableName = "RebusSagaIndex",
            string containerName = "RebusSagaStorage")
        {
            var storageAccount = AzureConfigurationHelper.GetStorageAccount(storageAccountConnectionStringOrName);

            StoreInAzureStorage(configurer, storageAccount, tableName, containerName);
        }

        /// <summary>
        /// Configures Rebus to use a combination of blob and table storage to store sagas
        /// </summary>
        public static void StoreInAzureStorage(this StandardConfigurer<ISagaStorage> configurer, CloudStorageAccount cloudStorageAccount,
            string tableName = "RebusSagaIndex",
            string containerName = "RebusSagaStorage")
        {
            configurer.Register(c => new AzureStorageSagaStorage(cloudStorageAccount, c.Get<IRebusLoggerFactory>(), tableName, containerName));
        }

        /// <summary>
        /// Configures Rebus to store saga data snapshots in blob storage
        /// </summary>
        public static void StoreInBlobStorage(this StandardConfigurer<ISagaSnapshotStorage> configurer, CloudStorageAccount cloudStorageAccount, string containerName = "RebusSagaStorage")
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));
            if (cloudStorageAccount == null) throw new ArgumentNullException(nameof(cloudStorageAccount));
            if (containerName == null) throw new ArgumentNullException(nameof(containerName));

            configurer.Register(c => new AzureStorageSagaSnapshotStorage(cloudStorageAccount, c.Get<IRebusLoggerFactory>(), containerName));
        }
    }
}