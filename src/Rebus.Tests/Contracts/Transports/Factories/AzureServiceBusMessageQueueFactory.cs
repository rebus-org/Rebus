using System;
using Rebus.AzureServiceBus;

namespace Rebus.Tests.Contracts.Transports.Factories
{
    public class AzureServiceBusMessageQueueFactory : ITransportFactory
    {

        string connectionString = "";
            

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

        private AzureServiceBusMessageQueue GetQueue(string queueName)
        {
            return new AzureServiceBusMessageQueue(connectionString, queueName, "error").ResetQueue();
        }
    }
}