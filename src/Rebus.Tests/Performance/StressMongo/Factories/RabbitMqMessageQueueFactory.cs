using System;
using System.Collections.Generic;
using Rebus.RabbitMQ;
using Rebus.Tests.Transports.Rabbit;

namespace Rebus.Tests.Performance.StressMongo.Factories
{
    public class RabbitMqMessageQueueFactory : IMessageQueueFactory
    {
        readonly List<IDisposable> disposables = new List<IDisposable>();

        public Tuple<ISendMessages, IReceiveMessages> GetQueue(string inputQueueName)
        {
            var rabbitMqMessageQueue = new RabbitMqMessageQueue(RabbitMqFixtureBase.ConnectionString, inputQueueName, "error").PurgeInputQueue();
            disposables.Add(rabbitMqMessageQueue);
            return new Tuple<ISendMessages, IReceiveMessages>(rabbitMqMessageQueue, rabbitMqMessageQueue);
        }

        public void CleanUp()
        {
            disposables.ForEach(d => d.Dispose());
        }
    }
}