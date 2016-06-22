using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Rebus.AzureStorage.Subscriptions;
using Rebus.AzureStorage.Tests.Transport;
using Rebus.Subscriptions;
using Rebus.Tests.Contracts.Subscriptions;

namespace Rebus.AzureStorage.Tests.Subscriptions
{
    public class AzureStorageSubscriptionStorageFactory : AzureStorageFactoryBase, ISubscriptionStorageFactory
    {
        private static readonly string TableName = $"RebusSubscriptionsTest{DateTime.Now:yyyyMMddHHmmss}";
        public ISubscriptionStorage Create()
        {
            return new AzureStorageSubscriptionStorage(StorageAccount, false,
                    TableName);
            
        }



        public void Cleanup()
        {
            DropTable(TableName);
        }

        public static void CreateTables()
        {
            var sub = new AzureStorageSubscriptionStorage(StorageAccount, false, TableName);
            sub.EnsureCreated();
        }

        public static void DropTables()
        {
            var sub = new AzureStorageSubscriptionStorage(StorageAccount, false, TableName);
            sub.DropTables();
        }
    }
}
