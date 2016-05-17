using System;
using System.Configuration;
using GoCommando;
using GoCommando.Attributes;
using Microsoft.WindowsAzure.Storage;
using Rebus.AzureStorage.Transport;
using Rebus.Forklift.Common;

namespace Rebus.Forklift.AzureStorageQueues
{
    [Banner(@"Rebus Forklift - simple message mover - Azure Storage Queues edition")]
    class Program : ForkliftBase
    {
        [PositionalArgument]
        [Description("Azure Storage Account connection string name")]
        [Example("storage")]
        public string ConnectionStringName { get; set; }

        static void Main(string[] args)
        {
            Go.Run<Program>(args);
        }

        protected override void DoRun()
        {
            var transport = new AzureStorageQueuesTransport(GetStorageAccount(ConnectionStringName), InputQueue, LoggerFactory);

            var returnToSourceQueue = new ReturnToSourceQueue(transport)
            {
                DefaultOutputQueue = DefaultOutputQueue,
                InputQueue = InputQueue
            };

            returnToSourceQueue.Run();
        }

        static CloudStorageAccount GetStorageAccount(string connectionStringName)
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionStringName];

            if (connectionStringSettings != null)
            {
                Text.PrintLine("Using connection string named '{0}' from app.config", connectionStringName);

                return CloudStorageAccount.Parse(connectionStringSettings.ConnectionString);
            }

            var environmentVariable = Environment.GetEnvironmentVariable(connectionStringName);

            if (environmentVariable != null)
            {
                Text.PrintLine("Using connection string '{0}' from ENV variable", connectionStringName);

                return CloudStorageAccount.Parse(environmentVariable);
            }

            throw new ArgumentException(string.Format("Could not find connection string named '{0}' in the current app.config or as an ENV variable!", connectionStringName));
        }
    }
}
