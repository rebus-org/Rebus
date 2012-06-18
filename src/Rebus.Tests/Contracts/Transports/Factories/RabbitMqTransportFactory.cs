using System;
using System.Collections.Generic;
using Rebus.RabbitMQ;
using Rebus.Tests.Transports.Rabbit;

namespace Rebus.Tests.Contracts.Transports.Factories
{
    public class RabbitMqTransportFactory : ITransportFactory
    {
        readonly List<IDisposable> disposables = new List<IDisposable>();

        public Tuple<ISendMessages, IReceiveMessages> Create()
        {
            var sender = GetQueue("tests.contracts.sender");
            var receiver = GetQueue("tests.contracts.receiver");

            return new Tuple<ISendMessages, IReceiveMessages>(sender, receiver);
        }

        RabbitMqMessageQueue GetQueue(string queueName)
        {
            var queue = new RabbitMqMessageQueue(RabbitMqFixtureBase.ConnectionString, queueName, queueName + ".error");
            queue.PurgeInputQueue();
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