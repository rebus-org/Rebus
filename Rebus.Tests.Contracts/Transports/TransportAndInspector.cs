using Rebus.Transport;

namespace Rebus.Tests.Contracts.Transports;

public class TransportAndInspector
{
    public TransportAndInspector(ITransport transport, ITransportInspector transportInspector)
    {
        Transport = transport;
        TransportInspector = transportInspector;
    }

    public ITransportInspector TransportInspector { get; }
    public ITransport Transport { get; }
}