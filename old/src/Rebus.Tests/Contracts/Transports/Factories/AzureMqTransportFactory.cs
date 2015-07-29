using System;
using Rebus.Azure;

namespace Rebus.Tests.Contracts.Transports.Factories
{
    public class AzureMqTransportFactory : ITransportFactory
    {
        AzureMessageQueue sender;
        AzureMessageQueue receiver;

        public Tuple<ISendMessages, IReceiveMessages> Create()
        {
            sender = GetQueue("myTestSender");
            receiver = GetQueue("myTestReceiver");

            return new Tuple<ISendMessages, IReceiveMessages>(sender, receiver);
        }

        static AzureMessageQueue GetQueue(string inputQueueName)
        {
            return new AzureMessageQueue(AzureUtil.CloudStorageAccount, inputQueueName, "error").PurgeInputQueue();
        }

        public IReceiveMessages CreateReceiver(string queueName)
        {
            return GetQueue(queueName);
        }

        public void CleanUp()
        {
        }
    }
}