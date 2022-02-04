using Rebus.Transport;

namespace Rebus.Tests.Contracts.Transports;

public interface ITransportFactory
{
    ITransport CreateOneWayClient();
    ITransport Create(string inputQueueAddress);
    void CleanUp();
}