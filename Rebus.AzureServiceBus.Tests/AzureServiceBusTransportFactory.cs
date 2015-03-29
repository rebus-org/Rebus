using System;
using System.Collections.Generic;
using Microsoft.ServiceBus;
using NUnit.Framework;
using Rebus.Extensions;
using Rebus.Tests.Contracts.Transports;
using Rebus.Transport;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture]
    public class AzureServiceBusBasicSendReceive : BasicSendReceive<AzureServiceBusTransportFactory> { }

    public class AzureServiceBusTransportFactory : ITransportFactory
    {
        public const string ConnectionString = "Endpoint=sb://rebus2.servicebus.windows.net/;SharedAccessKeyName=Tests;SharedAccessKey=Z3/e1CLzRYSX1SWVHIv0W3nZPp3n6DHcL/gDG5E8BO4=";
        
        readonly Dictionary<string, AzureServiceBusTransport> _queuesToDelete = new Dictionary<string, AzureServiceBusTransport>();

        public ITransport Create(string inputQueueAddress)
        {
            return _queuesToDelete.GetOrAdd(inputQueueAddress, () =>
            {
                var transport = new AzureServiceBusTransport(ConnectionString, inputQueueAddress);

                transport.PurgeInputQueue();

                transport.Initialize();

                return transport;
            });
        }

        public void CleanUp()
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(ConnectionString);

            _queuesToDelete.Keys.ForEach(queueName =>
            {
                if (!namespaceManager.QueueExists(queueName)) return;

                Console.WriteLine("Deleting ASB queue {0}", queueName);

                namespaceManager.DeleteQueue(queueName);
            });
        }
    }
}
