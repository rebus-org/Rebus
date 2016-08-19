using Microsoft.WindowsAzure.Storage.Table;

namespace Rebus.AzureStorage.Entities
{
    public class AzureStorageSubscription : TableEntity
    {
        public AzureStorageSubscription(string topic, string subscriberAddress)
        {
            PartitionKey = topic;
            RowKey = subscriberAddress;
        }
        public AzureStorageSubscription() { }
    }
}
