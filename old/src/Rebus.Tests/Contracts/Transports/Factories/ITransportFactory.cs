using System;

namespace Rebus.Tests.Contracts.Transports.Factories
{
    public interface ITransportFactory
    {
        Tuple<ISendMessages, IReceiveMessages> Create();
        void CleanUp();
        IReceiveMessages CreateReceiver(string queueName);
    }
}