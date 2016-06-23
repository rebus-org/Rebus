using System;
using Rebus.AzureStorage.DataBus;
using Rebus.DataBus;
using Rebus.Logging;
using Rebus.Tests.Contracts.DataBus;

namespace Rebus.AzureStorage.Tests.DataBus
{
    public class AzureBlobsDataBusStorageFactory : IDataBusStorageFactory
    {
        readonly string _containerName = $"container-{Guid.NewGuid().ToString().Substring(0, 3)}".ToLowerInvariant();

        public IDataBusStorage Create()
        {
            Console.WriteLine($"Creating blobs data bus storage for container {_containerName}");

            return new AzureBlobsDataBusStorage(AzureConfig.StorageAccount, _containerName, new ConsoleLoggerFactory(false));
        }

        public void CleanUp()
        {
            Console.WriteLine($"Deleting container {_containerName} (if it exists)");

            AzureConfig.StorageAccount.CreateCloudBlobClient()
                .GetContainerReference(_containerName)
                .DeleteIfExists();
        }
    }
}