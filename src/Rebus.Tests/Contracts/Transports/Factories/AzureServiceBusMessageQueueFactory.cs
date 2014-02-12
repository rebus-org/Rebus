using System;
using System.IO;
using Rebus.AzureServiceBus.Queues;

namespace Rebus.Tests.Contracts.Transports.Factories
{
    public class AzureServiceBusMessageQueueFactory : ITransportFactory
    {
        public static string ConnectionString
        {
            get
            {
                var connectionStringFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "azure_connection_string.txt");
                
                if (!File.Exists(connectionStringFile))
                {
                    throw new ArgumentException(
                        string.Format(@"Could not find text file with Azure connection string - looked here: {0}

If you want to run the Azure tests, please provide a text file containing a valid connection string, e.g. something like

Endpoint=sb://someServiceBusSomewhere.servicebus.windows.net/;SharedAccessKeyName=SomeAccessKeyThatCanAccesTopic;SharedAccessKey=baef57deadbputthekeyinhereb5eb8dfdef8ad

",
                                      connectionStringFile));
                }

                return File.ReadAllText(connectionStringFile).Trim("\r\n ".ToCharArray());
            }
        }

        public Tuple<ISendMessages, IReceiveMessages> Create()
        {
            var sender = GetQueue("myTestSender");
            var receiver = GetQueue("myTestReceiver");

            return new Tuple<ISendMessages, IReceiveMessages>(sender, receiver);
        }

        public void CleanUp()
        {
            
        }

        public IReceiveMessages CreateReceiver(string queueName)
        {
            return GetQueue(queueName);
        }

        AzureServiceBusMessageQueue GetQueue(string queueName)
        {
            return new AzureServiceBusMessageQueue(ConnectionString, queueName).Purge();
        }
    }
}