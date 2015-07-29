using System;
using Rebus.AzureServiceBus;

namespace Rebus.Tests.Contracts.Transports.Factories
{
    public class AzureServiceBusMessageQueueFactory : ITransportFactory
    {
        public static string ConnectionString
        {
            get { return AzureUtil.AzureServiceBusConnectionString; }
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