using System;
using Rebus.RabbitMQ;
using Rebus.Tests.Transports.Rabbit;
using Rebus.Transports.Msmq;

namespace Rebus.Tests.Contracts
{
    public interface ITransportFactory
    {
        Tuple<ISendMessages, IReceiveMessages> Create();
        void CleanUp();
    }

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

    public class MsmqTransportFactory : ITransportFactory
    {
        MsmqMessageQueue sender;
        MsmqMessageQueue receiver;

        public Tuple<ISendMessages, IReceiveMessages> Create()
        {
            sender = new MsmqMessageQueue(@"test.contracts.sender", "error").PurgeInputQueue();
            receiver = new MsmqMessageQueue(@"test.contracts.receiver", "error").PurgeInputQueue();
            return new Tuple<ISendMessages, IReceiveMessages>(sender, receiver);
        }

        public void CleanUp()
        {
            sender.Dispose();
            receiver.Dispose();
        }
    }
}