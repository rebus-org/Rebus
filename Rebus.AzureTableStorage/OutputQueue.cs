using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;

namespace Rebus.AzureTableStorage
{
    class InMemOutputQueue : ConcurrentDictionary<string, TableBatchOperation>
    {


        public void AddMessage(string destinationAddress, TransportMessageEntity message)
        {

            AddOrUpdate(destinationAddress.ToLowerInvariant(),
                (key) =>
                {
                    var operation = new TableBatchOperation();
                    operation.Insert(message);
                    return operation;
                },
                (key, operation) =>
                {
                    operation.Insert(message);
                    return operation;
                });

        }

        public IEnumerable<InMemDestinationBatch> GetBatchOperations()
        {

            return ToArray().Select(lists => new InMemDestinationBatch(lists.Key, lists.Value));
        }






    }

    class InMemDestinationBatch
    {
        public InMemDestinationBatch(string destinationAddress, TableBatchOperation operation)
        {
            DestinationAddress = destinationAddress;
            Operation = operation;
        }

        public string DestinationAddress { get; private set; }
        public TableBatchOperation Operation { get; private set; }
    }
}