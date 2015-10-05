using System;
using System.Configuration;
using GoCommando;
using GoCommando.Attributes;
using Rebus.AzureServiceBus;
using Rebus.Forklift.Common;

namespace Rebus.Forklift.AzureServiceBus
{
    [Banner(@"Rebus Forklift - simple message mover - Azure Service Bus edition")]
    class Program : ForkliftBase
    {
        [PositionalArgument]
        [Description("Azure Service Bus connection string name")]
        [Example("asb")]
        [Example("Microsoft.ServiceBus")]
        public string ConnectionStringName { get; set; }

        static void Main(string[] args)
        {
            Go.Run<Program>(args);
        }

        protected override void DoRun()
        {
            using (var transport = new AzureServiceBusTransport(GetConnectionString(ConnectionStringName), InputQueue, LoggerFactory))
            {
                var returnToSourceQueue = new ReturnToSourceQueue(transport)
                {
                    InputQueue = InputQueue,
                    DefaultOutputQueue = DefaultOutputQueue
                };

                returnToSourceQueue.Run();
            }
        }

        static string GetConnectionString(string connectionStringName)
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionStringName];

            if (connectionStringSettings != null)
            {
                Text.PrintLine("Using connection string '{0}' from app.config", connectionStringName);

                return connectionStringSettings.ConnectionString;
            }

            var environmentVariable = Environment.GetEnvironmentVariable(connectionStringName);

            if (environmentVariable != null)
            {
                Text.PrintLine("Using connection string '{0}' from ENV variable", connectionStringName);

                return environmentVariable;
            }

            throw new ArgumentException(string.Format("Could not find '{0}' among the available connection strings in app.config or as an ENV variable", connectionStringName));
        }
    }
}
