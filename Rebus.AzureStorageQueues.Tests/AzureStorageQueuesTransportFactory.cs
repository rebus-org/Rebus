using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Rebus.Tests.Contracts.Transports;
using Rebus.Transport;

namespace Rebus.AzureStorageQueues.Tests
{
    public class AzureStorageQueuesTransportFactory : ITransportFactory
    {
        readonly ConcurrentDictionary<string, AzureStorageQueuesTransport> _transports = new ConcurrentDictionary<string, AzureStorageQueuesTransport>(StringComparer.InvariantCultureIgnoreCase);

        public ITransport Create(string inputQueueAddress)
        {
            return _transports.GetOrAdd(inputQueueAddress, address =>
            {
                var storageAccount = CloudStorageAccount.Parse(ConnectionString);
                var transport = new AzureStorageQueuesTransport(storageAccount, inputQueueAddress);

                transport.PurgeInputQueue();

                transport.Initialize();

                return transport;
            });
        }

        public static string ConnectionString
        {
            get
            {
                return ConnectionStringFromFileOrNull(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "azure_storage_connection_string.txt"))
                       ?? ConnectionStringFromEnvironmentVariable("rebus2_storage_connection_string")
                       ?? Throw("Could not find Azure Storage connection string!");
            }
        }

        static string ConnectionStringFromFileOrNull(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Could not find file {0}", filePath);
                return null;
            }

            Console.WriteLine("Using Azure Storage connection string from file {0}", filePath);
            return File.ReadAllText(filePath);
        }

        static string ConnectionStringFromEnvironmentVariable(string environmentVariableName)
        {
            var value = Environment.GetEnvironmentVariable(environmentVariableName);

            if (value == null)
            {
                Console.WriteLine("Could not find env variable {0}", environmentVariableName);
                return null;
            }

            Console.WriteLine("Using Azure Storage connection string from env variable {0}", environmentVariableName);

            return value;
        }

        static string Throw(string message)
        {
            throw new ConfigurationErrorsException(message);
        }

        public void CleanUp()
        {
        }
    }
}
