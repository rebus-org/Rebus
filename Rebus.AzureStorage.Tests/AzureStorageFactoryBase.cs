using System;
using System.Configuration;
using System.IO;
using Microsoft.WindowsAzure.Storage;

namespace Rebus.AzureStorage.Tests.Transport
{
    public class AzureStorageFactoryBase
    {
        public static string ConnectionString => ConnectionStringFromFileOrNull(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "azure_storage_connection_string.txt"))
                                                 ?? ConnectionStringFromEnvironmentVariable("rebus2_storage_connection_string")
                                                 ?? AzureStorageQueuesTransportFactory.Throw("Could not find Azure Storage connection string!");

        private static string ConnectionStringFromFileOrNull(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Could not find file {0}", filePath);
                return null;
            }

            Console.WriteLine("Using Azure Storage connection string from file {0}", filePath);
            return File.ReadAllText(filePath);
        }

        private static string ConnectionStringFromEnvironmentVariable(string environmentVariableName)
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
        protected static CloudStorageAccount StorageAccount
        {
            get { return CloudStorageAccount.Parse(ConnectionString); }
        }

        protected static void DropTable(string tableName)
        {
            var client = StorageAccount.CreateCloudTableClient();

            var table = client.GetTableReference(tableName);
            table.DeleteIfExists();
        }

        protected static void DropContainer(string containerName)
        {
            var client = StorageAccount.CreateCloudBlobClient();
            var container = client.GetContainerReference(containerName);
            container.DeleteIfExists();
        }
    }
}