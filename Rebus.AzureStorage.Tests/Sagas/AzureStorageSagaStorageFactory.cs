using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rebus.AzureStorage.Sagas;
using Rebus.AzureStorage.Tests.Transport;
using Rebus.Logging;
using Rebus.Sagas;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.AzureStorage.Tests.Sagas
{
    public class AzureStorageSagaStorageFactory : AzureStorageFactoryBase, ISagaStorageFactory//, ISagaSnapshotStorageFactory
    {
        public static readonly string ContainerName = $"RebusSagaStorageTestContainer";
        public static readonly string TableName = $"RebusSagaStorageTestTable";


        public ISagaStorage GetSagaStorage()
        {
            lock (ContainerName)
            {
                var s = new AzureStorageSagaStorage(StorageAccount, new ConsoleLoggerFactory(false), TableName, ContainerName);

                return s;
            }
        }

        public void CleanUp()
        {

        }


        //public static void DropAndRecreateObjects()
        //{
        //    var cloudTableClient = StorageAccount.CreateCloudTableClient();

        //    var table = cloudTableClient.GetTableReference(TableName);
        //    table.DeleteIfExists();
        //    var cloudBlobClient = StorageAccount.CreateCloudBlobClient();
        //    var container = cloudBlobClient.GetContainerReference(ContainerName.ToLowerInvariant());
        //    container.DeleteIfExists();
        //}

    }
}
