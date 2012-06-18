using System;
using Rebus.RabbitMQ;
using Rebus.Tests.Transports.Rabbit;

namespace Rebus.Tests.Contracts.Transports.Factories
{
    public class RabbitMqTransportFactory : ITransportFactory
    {
        RabbitMqMessageQueue sender;
        RabbitMqMessageQueue receiver;

        public Tuple<ISendMessages, IReceiveMessages> Create()
        {
            sender = new RabbitMqMessageQueue(RabbitMqFixtureBase.ConnectionString, "tests.contracts.sender", "tests.contracts.sender.error");
            receiver = new RabbitMqMessageQueue(RabbitMqFixtureBase.ConnectionString, "tests.contracts.receiver", "tests.contracts.receiver.error");
            
            return new Tuple<ISendMessages, IReceiveMessages>(sender, receiver);
        }

        public void CleanUp()
        {
            sender.Dispose();
            receiver.Dispose();
        }
    }
}