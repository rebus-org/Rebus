using Microsoft.WindowsAzure.Storage;
using Rebus.AzureStorage.Subscriptions;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Subscriptions;

namespace Rebus.AzureStorage.Config
{
    public static class AzureStorageSubscriptionConfigurationExtensions
    {
        public static void UseAzure(this StandardConfigurer<ISubscriptionStorage> configurer,
            string storageAccountConnectionStringOrName, string tableName = "RebusSubscriptions", bool isCentralized = false)
        {
            if (!storageAccountConnectionStringOrName.ToLowerInvariant().Contains("accountkey="))
            {
                storageAccountConnectionStringOrName =
                    System.Configuration.ConfigurationManager.ConnectionStrings[storageAccountConnectionStringOrName]
                        .ConnectionString;
            }
            var storageAccount = CloudStorageAccount.Parse(storageAccountConnectionStringOrName);
            Register(configurer,tableName, storageAccount, isCentralized);
        }
        public static void UseAzure(this StandardConfigurer<ISubscriptionStorage> configurer,
            CloudStorageAccount storageAccount, string tableName = "RebusSubscriptions", bool isCentralized = false)
        {
            Register(configurer, tableName, storageAccount, isCentralized);
        }
        static void Register(StandardConfigurer<ISubscriptionStorage> configurer, string tableName, CloudStorageAccount storageAccount, bool isCentralized = false)
        {
            configurer.Register(c => new AzureStorageSubscriptionStorage(storageAccount, c.Get<IRebusLoggerFactory>(), isCentralized, tableName));

 
        }
    }
}