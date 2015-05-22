using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using Rebus.Extensions;
using Rebus.Tests.Contracts.Transports;
using Rebus.Transport;

namespace Rebus.AzureTableStorage.Tests
{
    public class AzureTableStorageTransportFactory : ITransportFactory
    {
        public static string ConnectionString
        {
            get
            {
                return ConnectionStringFromFileOrNull(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "ats_connection_string.txt"))
                       ?? ConnectionStringFromEnvironmentVariable("rebus2_ats_connection_string")
                       ?? Throw("Could not find Azure table storage connetion string!");
            }
        }

        static string Throw(string message)
        {
            throw new ConfigurationErrorsException(message);
        }

        static string ConnectionStringFromEnvironmentVariable(string environmentVariableName)
        {
            var value = Environment.GetEnvironmentVariable(environmentVariableName);

            if (value == null)
            {
                Console.WriteLine("Could not find env variable {0}", environmentVariableName);
                return null;
            }

            Console.WriteLine("Using Azure Table storage connection string from env variable {0}", environmentVariableName);

            return value;
        }

        static string ConnectionStringFromFileOrNull(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Could not find file {0}", filePath);
                return null;
            }

            Console.WriteLine("Using Azure Table Storage connection string from file {0}", filePath);
            return File.ReadAllText(filePath);
        }

        readonly Dictionary<string, AzureTableStorageTransport> _queuesToDelete = new Dictionary<string, AzureTableStorageTransport>();

        public ITransport Create(string inputQueueAddress)
        {
            return _queuesToDelete.GetOrAdd(inputQueueAddress, () =>
            {
                var transport = new AzureTableStorageTransport(ConnectionString, inputQueueAddress);

                transport.PurgeInputQueue();

                transport.Initialize();

                return transport;
            });
        }

        public void CleanUp()
        {
            //var namespaceManager = NamespaceManager.CreateFromConnectionString(ConnectionString);

            //_queuesToDelete.Keys.ForEach(queueName =>
            //{
            //    if (!namespaceManager.QueueExists(queueName)) return;

            //    Console.WriteLine("Deleting ASB queue {0}", queueName);

            //    namespaceManager.DeleteQueue(queueName);
            //});
        }
    }
}
