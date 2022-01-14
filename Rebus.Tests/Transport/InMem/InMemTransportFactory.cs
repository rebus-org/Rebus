using Rebus.Tests.Contracts.Transports;
using Rebus.Transport;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Transport.InMem;

public class InMemTransportFactory : ITransportFactory
{
    readonly InMemNetwork _network = new InMemNetwork();

    public ITransport CreateOneWayClient()
    {
        return Create(null);
    }

    public ITransport Create(string inputQueueAddress)
    {
        var transport = new InMemTransport(_network, inputQueueAddress);
        transport.Initialize();
        return transport;
    }

    public void CleanUp()
    {
    }
}