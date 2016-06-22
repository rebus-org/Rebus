using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Rebus.AzureStorage.Transport;
using Rebus.Logging;
using Rebus.Tests.Contracts.Transports;
using Rebus.Transport;

namespace Rebus.AzureStorage.Tests.Transport
{
    public class AzureStorageQueuesTransportFactory : AzureStorageFactoryBase, ITransportFactory
    {
        readonly ConcurrentDictionary<string, AzureStorageQueuesTransport> _transports = new ConcurrentDictionary<string, AzureStorageQueuesTransport>(StringComparer.InvariantCultureIgnoreCase);

        public ITransport CreateOneWayClient()
        {
            return Create(null);
        }

        public ITransport Create(string inputQueueAddress)
        {
            if (inputQueueAddress == null)
            {
                var storageAccount = CloudStorageAccount.Parse(ConnectionString);
                var transport = new AzureStorageQueuesTransport(storageAccount, null, new ConsoleLoggerFactory(false));

                transport.Initialize();

                return transport;
            }

            return _transports.GetOrAdd(inputQueueAddress, address =>
            {
                var storageAccount = CloudStorageAccount.Parse(ConnectionString);
                var transport = new AzureStorageQueuesTransport(storageAccount, inputQueueAddress, new ConsoleLoggerFactory(false));

                transport.PurgeInputQueue();

                transport.Initialize();

                return transport;
            });
        }



        public void CleanUp()
        {
        }
    }
}
