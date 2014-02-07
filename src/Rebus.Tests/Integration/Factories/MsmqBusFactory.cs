using Rebus.Shared;
using Rebus.Transports.Msmq;

namespace Rebus.Tests.Integration.Factories
{
    class MsmqBusFactory : BusFactoryBase
    {
        protected override IDuplexTransport CreateTransport(string inputQueueName)
        {
            MsmqUtil.EnsureMessageQueueExists(MsmqUtil.GetPath(ErrorQueueName));
            return new MsmqMessageQueue(inputQueueName).PurgeInputQueue();
        }
    }
}