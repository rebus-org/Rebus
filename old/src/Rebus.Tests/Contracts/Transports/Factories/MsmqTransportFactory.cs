using System;
using System.Collections.Generic;
using Rebus.Transports.Msmq;

namespace Rebus.Tests.Contracts.Transports.Factories
{
    public class MsmqTransportFactory : ITransportFactory
    {
        readonly List<IDisposable> disposables = new List<IDisposable>();

        public Tuple<ISendMessages, IReceiveMessages> Create()
        {
            var sender = GetQueue(@"test.contracts.sender");
            var receiver = GetQueue(@"test.contracts.receiver");

            return new Tuple<ISendMessages, IReceiveMessages>(sender, receiver);
        }

        MsmqMessageQueue GetQueue(string queueName)
        {
            var queue = new MsmqMessageQueue(queueName).PurgeInputQueue();
            
            disposables.Add(queue);
            
            return queue;
        }

        public void CleanUp()
        {
            disposables.ForEach(d => d.Dispose());
        }

        public IReceiveMessages CreateReceiver(string queueName)
        {
            return GetQueue(queueName);
        }
    }
}