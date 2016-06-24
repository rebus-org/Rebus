using System;
using Microsoft.WindowsAzure.Storage;
using Rebus.AzureStorage.Subscriptions;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Subscriptions;

namespace Rebus.AzureStorage.Config
{
    /// <summary>
    /// Configuration extensions for Azure storage-based subscriptions
    /// </summary>
    public static class AzureStorageSubscriptionConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to store subscriptions using Azure Table Storage
        /// </summary>
        public static void StoreInTableStorage(this StandardConfigurer<ISubscriptionStorage> configurer, string storageAccountConnectionStringOrName, string tableName = "RebusSubscriptions", bool isCentralized = false)
        {
            var cloudStorageAccount = AzureConfigurationHelper.GetStorageAccount(storageAccountConnectionStringOrName);

            Register(configurer, tableName, cloudStorageAccount, isCentralized);
        }

        /// <summary>
        /// Configures Rebus to store subscriptions using Azure Table Storage
        /// </summary>
        public static void StoreInTableStorage(this StandardConfigurer<ISubscriptionStorage> configurer, CloudStorageAccount storageAccount, string tableName = "RebusSubscriptions", bool isCentralized = false)
        {
            Register(configurer, tableName, storageAccount, isCentralized);
        }

        static void Register(StandardConfigurer<ISubscriptionStorage> configurer, string tableName, CloudStorageAccount storageAccount, bool isCentralized = false)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));
            if (tableName == null) throw new ArgumentNullException(nameof(tableName));
            if (storageAccount == null) throw new ArgumentNullException(nameof(storageAccount));

            configurer.Register(c => new AzureStorageSubscriptionStorage(storageAccount, c.Get<IRebusLoggerFactory>(), isCentralized, tableName));
        }
    }
}