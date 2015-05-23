using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;

namespace Rebus.AzureTableStorage
{
    class TransportMessageEntity : TableEntity
    {
        public TransportMessageEntity()
        {

        }
        private readonly DateTime MinLeaseDate = new DateTime(1700, 1, 1);
        public TransportMessageEntity(string partitionKey, string rowKey, Dictionary<string, string> headers, byte[] body)
        {
            if (partitionKey == null) throw new ArgumentNullException("partitionKey");
            if (rowKey == null) throw new ArgumentNullException("rowKey");
            if (headers == null) throw new ArgumentNullException("headers");
            if (body == null) throw new ArgumentNullException("body");

            var keyValues = headers.Select(kv => kv.Key + "€" + kv.Value);
            HeaderString = String.Join("¤", keyValues);
            Body = body;
            PartitionKey = partitionKey;
            RowKey = rowKey;
            LeaseTimeout = MinLeaseDate;
            SentTime = Time.RebusTime.Now;
        }

        public Dictionary<string, string> GetHeaders()
        {
            var keyValues = HeaderString.Split('¤');

            return keyValues.ToDictionary(s => s.Split('€')[0], v => v.Split('€')[1]);
        }
        public string HeaderString { get; set; }
        public DateTimeOffset SentTime { get; set; }
        public DateTime LeaseTimeout { get; set; }
        /// <summary>
        /// Gets the wrapped body data of this message
        /// </summary>
        public byte[] Body { get; set; }

        public void ResetLease()
        {
            LeaseTimeout = MinLeaseDate;
        }
    }
}