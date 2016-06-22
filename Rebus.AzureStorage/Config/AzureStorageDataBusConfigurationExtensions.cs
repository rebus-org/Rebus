using Microsoft.WindowsAzure.Storage;
using Rebus.AzureStorage.Databus;
using Rebus.Config;
using Rebus.DataBus;

namespace Rebus.AzureStorage.Config
{
    public static class AzureStorageDataBusConfigurationExtensions
    {
        public static void UseAzureStorage(this StandardConfigurer<IDataBusStorage> configurer,
            string storageAccountConnectionStringOrName, string containerName = "RebusDataBusData")
        {
            if (!storageAccountConnectionStringOrName.ToLowerInvariant().Contains("accountkey="))
            {
                storageAccountConnectionStringOrName =
                    System.Configuration.ConfigurationManager.ConnectionStrings[storageAccountConnectionStringOrName]
                        .ConnectionString;
            }
            var storageAccount = CloudStorageAccount.Parse(storageAccountConnectionStringOrName);
            UseAzureStorage(configurer, storageAccount, containerName);
        }
        public static void UseAzureStorage(this StandardConfigurer<IDataBusStorage> configurer,
            CloudStorageAccount cloudStorageAccount, string containerName = "RebusDataBusData")
        {
            configurer.Register(c=>new AzureStorageDataBusStorage(cloudStorageAccount, containerName));
        }
    }
}