using Rebus.Transport;

namespace Rebus.Tests.Contracts.Transports
{
    public interface ITransportFactory
    {
        ITransport Create(string inputQueueAddress);
        void CleanUp();
    }
}