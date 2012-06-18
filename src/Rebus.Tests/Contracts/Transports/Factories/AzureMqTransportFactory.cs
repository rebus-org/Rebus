using System;
using Microsoft.WindowsAzure;
using Rebus.Azure;

namespace Rebus.Tests.Contracts.Transports.Factories
{
    public class AzureMqTransportFactory : ITransportFactory
    {
        AzureMessageQueue sender;
        AzureMessageQueue receiver;

        public Tuple<ISendMessages, IReceiveMessages> Create()
        {
            sender = new AzureMessageQueue(CloudStorageAccount.DevelopmentStorageAccount, "myTestSender", "testSenderError").PurgeInputQueue();
            receiver = new AzureMessageQueue(CloudStorageAccount.DevelopmentStorageAccount, "myTestReceiver", "testReceiverError").PurgeInputQueue();

            return new Tuple<ISendMessages, IReceiveMessages>(sender, receiver);
        }

        public void CleanUp()
        {
        }
    }
}