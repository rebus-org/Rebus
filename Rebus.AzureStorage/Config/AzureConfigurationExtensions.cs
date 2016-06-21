using Microsoft.WindowsAzure.Storage;
using Rebus.Auditing.Sagas;
using Rebus.AzureStorage.Databus;
using Rebus.Config;
using Rebus.DataBus;

namespace Rebus.AzureStorage.Config
{
    public static class AzureConfigurationExtensions
    {
        public static RebusConfigurer UseAzure(this RebusConfigurer conf, string storageAccountConnectionString, string inputQueueAddress)
        {
            var storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
            return UseAzure(conf, storageAccount, inputQueueAddress);
        }

        private static RebusConfigurer UseAzure(this RebusConfigurer conf, CloudStorageAccount storageAccount, string inputQueueAddress, 
            string subscriptionTableName  = "RebusSubscriptions",
            string sagaIndexTableName = "RebusSagaIndex",
            string sagaContainerName = "RebusSagaStorage",
            string dataBusContainerName = "RebusDataBusData",
            bool isCentralizedSubscriptions = false,
            bool enableDataBus = true,
            bool enableSagaSnapshots = true)
        {
            return conf.Transport(t => t.UseAzureStorageQueues(storageAccount, inputQueueAddress))
                .Subscriptions(t => t.UseAzure(storageAccount, subscriptionTableName, isCentralizedSubscriptions))
                .Sagas(t=>t.UseAzureStorage(storageAccount, sagaIndexTableName, sagaContainerName))
                .Options(o =>
                {
                    if (enableDataBus)
                    {
                        o.EnableDataBus().UseAzureStorage(storageAccount, dataBusContainerName);
                    }
                    if (enableSagaSnapshots)
                    {
                        o.EnableSagaAuditing().UseAzureStorage(storageAccount, sagaContainerName);
                    }
                });
        }
    }

    public static class AzureStorageDataBusConfigurationExtensions
    {
        public static void UseAzureStorage(this StandardConfigurer<IDataBusStorage> configurer,
            CloudStorageAccount cloudStorageAccount, string containerName = "RebusDataBusData")
        {
            configurer.Register(c=>new AzureStorageDataBusStorage(cloudStorageAccount, containerName));
        }
    }
}