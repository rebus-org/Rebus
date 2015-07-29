using Rebus.RabbitMQ;
using Rebus.Tests.Transports.Rabbit;

namespace Rebus.Tests.Integration.Factories
{
    class RabbitBusFactory : BusFactoryBase
    {
        protected override IDuplexTransport CreateTransport(string inputQueueName)
        {
            RegisterForDisposal(new DisposableAction(() => RabbitMqFixtureBase.DeleteQueue(inputQueueName)));
            return new RabbitMqMessageQueue(RabbitMqFixtureBase.ConnectionString, inputQueueName).PurgeInputQueue();
        }
    }
}