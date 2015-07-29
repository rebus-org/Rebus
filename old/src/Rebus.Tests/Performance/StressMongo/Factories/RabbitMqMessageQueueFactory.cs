using System;
using System.Collections.Generic;
using Rebus.Configuration;
using Rebus.RabbitMQ;
using Rebus.Tests.Transports.Rabbit;

namespace Rebus.Tests.Performance.StressMongo.Factories
{
    public class RabbitMqMessageQueueFactory : IMessageQueueFactory
    {
        readonly List<IDisposable> disposables = new List<IDisposable>();
        readonly List<string>  queuesToDelete = new List<string>();

        public Tuple<ISendMessages, IReceiveMessages> GetQueue(string inputQueueName)
        {
            var rabbitMqMessageQueue = new RabbitMqMessageQueue(RabbitMqFixtureBase.ConnectionString, inputQueueName).PurgeInputQueue();
            disposables.Add(rabbitMqMessageQueue);
            queuesToDelete.Add(inputQueueName);
            return new Tuple<ISendMessages, IReceiveMessages>(rabbitMqMessageQueue, rabbitMqMessageQueue);
        }

        public void CleanUp()
        {
            queuesToDelete.ForEach(RabbitMqFixtureBase.DeleteQueue);
            disposables.ForEach(d => d.Dispose());
        }

        public void ConfigureOneWayClientMode(RebusTransportConfigurer configurer)
        {
            configurer.UseRabbitMqInOneWayMode(RabbitMqFixtureBase.ConnectionString);
        }
    }
}