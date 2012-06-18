using System;
using Rebus.Transports.Msmq;

namespace Rebus.Tests.Contracts.Transports.Factories
{
    public class MsmqTransportFactory : ITransportFactory
    {
        MsmqMessageQueue sender;
        MsmqMessageQueue receiver;

        public Tuple<ISendMessages, IReceiveMessages> Create()
        {
            sender = new MsmqMessageQueue(@"test.contracts.sender").PurgeInputQueue();
            receiver = new MsmqMessageQueue(@"test.contracts.receiver").PurgeInputQueue();
            return new Tuple<ISendMessages, IReceiveMessages>(sender, receiver);
        }

        public void CleanUp()
        {
            sender.Dispose();
            receiver.Dispose();
        }
    }
}