using System;
using Rebus.AzureStorage.Subscriptions;
using Rebus.AzureStorage.Tests.Transport;
using Rebus.Logging;
using Rebus.Subscriptions;
using Rebus.Tests.Contracts.Subscriptions;

namespace Rebus.AzureStorage.Tests.Subscriptions
{
    public class AzureStorageSubscriptionStorageFactory : AzureStorageFactoryBase, ISubscriptionStorageFactory
    {
        static readonly string TableName = $"RebusSubscriptionsTest{DateTime.Now:yyyyMMddHHmmss}";

        public ISubscriptionStorage Create()
        {
            return new AzureStorageSubscriptionStorage(StorageAccount, new ConsoleLoggerFactory(false), false, TableName);
        }

        public void Cleanup()
        {
            DropTable(TableName);
        }

        public static void CreateTables()
        {
            var sub = new AzureStorageSubscriptionStorage(StorageAccount, new ConsoleLoggerFactory(false),  false, TableName);
            sub.EnsureCreated();
        }

        public static void DropTables()
        {
            var sub = new AzureStorageSubscriptionStorage(StorageAccount, new ConsoleLoggerFactory(false),  false, TableName);
            sub.DropTables();
        }
    }
}
