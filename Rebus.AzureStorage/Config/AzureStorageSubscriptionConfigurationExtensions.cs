using Microsoft.WindowsAzure.Storage;
using Rebus.AzureStorage.Subscriptions;
using Rebus.Config;
using Rebus.Subscriptions;

namespace Rebus.AzureStorage.Config
{
    public static class AzureStorageSubscriptionConfigurationExtensions
    {
        public static void UseAzure(this StandardConfigurer<ISubscriptionStorage> configurer,
            CloudStorageAccount storageAccount, string tableName = "RebusSubscriptions", bool isCentralized = false)
        {
            Register(configurer, tableName, storageAccount, isCentralized);
        }
        static void Register(StandardConfigurer<ISubscriptionStorage> configurer, string tableName, CloudStorageAccount storageAccount, bool isCentralized = false)
        {
            configurer.Register(c =>
            {
                return new AzureStorageSubscriptionStorage(storageAccount, isCentralized, tableName);
            });

 
        }
    }
}