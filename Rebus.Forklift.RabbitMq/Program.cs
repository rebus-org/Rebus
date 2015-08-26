using System.Configuration;
using GoCommando;
using GoCommando.Attributes;
using Rebus.Forklift.Common;
using Rebus.RabbitMq;

namespace Rebus.Forklift.RabbitMq
{
    [Banner(@"Rebus Forklift - simple message mover - RabbitMQ edition")]
    class Program : ForkliftBase
    {
        [NamedArgument("host", "host", Default = "amqp://localhost")]
        [Description("RabbitMQ connection string or name of connection string")]
        [Example("rabbit")]
        [Example("amqp://somehost")]
        public string HostnameOrConnectionString { get; set; }

        static void Main(string[] args)
        {
            Go.Run<Program>(args);
        }

        protected override void DoRun()
        {
            using (var transport = new RabbitMqTransport(GetConnectionString(HostnameOrConnectionString), InputQueue))
            {
                var returnToSourceQueue = new ReturnToSourceQueue(transport)
                {
                    InputQueue = InputQueue,
                    DefaultOutputQueue = DefaultOutputQueue
                };

                returnToSourceQueue.Run();
            }
        }

        static string GetConnectionString(string hostnameOrConnectionString)
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings[hostnameOrConnectionString];

            if (connectionStringSettings != null)
            {
                Text.PrintLine("Using connection string '{0}' from app.config", connectionStringSettings.Name);

                return connectionStringSettings.ConnectionString;
            }

            if (!hostnameOrConnectionString.StartsWith("amqp://"))
            {
                hostnameOrConnectionString = "amqp://" + hostnameOrConnectionString;
            }

            Text.PrintLine("Using connection string '{0}'", hostnameOrConnectionString);

            return hostnameOrConnectionString;
        }
    }
}
