using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;
using Rebus.AzureStorage.Entities;
using Rebus.Exceptions;
using Rebus.Logging;
using Rebus.Subscriptions;

namespace Rebus.AzureStorage.Subscriptions
{
    public class AzureStorageSubscriptionStorage : ISubscriptionStorage
    {
        readonly CloudStorageAccount _cloudStorageAccount;
        readonly IRebusLoggerFactory _loggerFactory;
        readonly string _tableName;

        public AzureStorageSubscriptionStorage(CloudStorageAccount cloudStorageAccount,
            IRebusLoggerFactory loggerFactory,
            bool isCentralized = false,
            string tableName = "RebusSubscriptions")
        {
            IsCentralized = isCentralized;
            _cloudStorageAccount = cloudStorageAccount;
            _loggerFactory = loggerFactory;
            _tableName = tableName;
        }

        public void EnsureCreated()
        {
            _loggerFactory.GetCurrentClassLogger().Info("Auto creating table {0}", _tableName);
            var client = _cloudStorageAccount.CreateCloudTableClient();
            var t = client.GetTableReference(_tableName);
            t.CreateIfNotExists();
        }

        CloudTable GetTable()
        {
            var client = _cloudStorageAccount.CreateCloudTableClient();
            return client.GetTableReference(_tableName);
        }

        // PartitionKey = Topic
        // RowKey = Address

        public async Task<string[]> GetSubscriberAddresses(string topic)
        {
            try
            {
                var query =
                    new TableQuery<AzureStorageSubscription>().Where(TableQuery.GenerateFilterCondition("PartitionKey",
                        QueryComparisons.Equal, topic));
                var t = GetTable();
                var operationContext = new OperationContext();
                var tableRequestOptions = new TableRequestOptions { RetryPolicy = new ExponentialRetry() };
                var items = await t.ExecuteQueryAsync(query, tableRequestOptions, operationContext);
                return items.Select(i => i.RowKey).ToArray();
            }
            catch (Microsoft.WindowsAzure.Storage.StorageException exception)
            {
                throw new RebusApplicationException(exception, $"Could not get subscriber addresses for '{topic}'");
            }
        }

        public async Task RegisterSubscriber(string topic, string subscriberAddress)
        {
            try
            {
                var entity = new Entities.AzureStorageSubscription(topic, subscriberAddress);
                var t = GetTable();
                var operationContext = new OperationContext();
                var tableRequestOptions = new TableRequestOptions { RetryPolicy = new ExponentialRetry() };
                var res = await t.ExecuteAsync(TableOperation.InsertOrReplace(entity), tableRequestOptions, operationContext);
            }
            catch (Exception exception)
            {
                throw new RebusApplicationException(exception, $"Could not subscribe {subscriberAddress} to '{topic}'");
            }
        }

        public async Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
            try
            {
                var entity = new Entities.AzureStorageSubscription(topic, subscriberAddress) { ETag = "*" };
                var t = GetTable();
                var operationContext = new OperationContext();
                var res = await t.ExecuteAsync(TableOperation.Delete(entity), new TableRequestOptions { RetryPolicy = new ExponentialRetry() }, operationContext);
            }
            catch (Exception exception)
            {
                throw new RebusApplicationException(exception, $"Could not unsubscribe {subscriberAddress} from '{topic}'");
            }
        }

        /// <summary>
        /// Gets whether this subscription storage is centralized (i.e. whether subscribers can register themselves directly)
        /// </summary>
        public bool IsCentralized { get; }

        public void DropTables()
        {
            var client = _cloudStorageAccount.CreateCloudTableClient();
            var t = client.GetTableReference(_tableName);
            t.DeleteIfExists();
        }
    }
}
