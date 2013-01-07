using System.Collections.Generic;
using Rebus.RabbitMQ;
using Rebus.Tests.Transports.Rabbit;

namespace Rebus.Tests.Integration.Factories
{
    class RabbitBusFactory : BusFactoryBase
    {
        readonly List<string> queuesToDelete = new List<string>(); 

        protected override IDuplexTransport CreateTransport(string inputQueueName)
        {
            queuesToDelete.Add(inputQueueName);

            return new RabbitMqMessageQueue(RabbitMqFixtureBase.ConnectionString, inputQueueName);
        }

        public override void Cleanup()
        {
            queuesToDelete.ForEach(RabbitMqFixtureBase.DeleteQueue);
            base.Cleanup();
        }
    }
}