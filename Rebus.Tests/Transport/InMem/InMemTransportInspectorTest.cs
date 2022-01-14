using NUnit.Framework;
using Rebus.Tests.Contracts.Transports;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Transport.InMem;

[TestFixture]
public class InMemTransportInspectorTest : TransportInspectorTest<InMemTransportInspectorTest.InMemTransportInspectorFactory>
{
    public class InMemTransportInspectorFactory : ITransportInspectorFactory
    {
        readonly InMemNetwork _network = new InMemNetwork();

        public TransportAndInspector Create(string address)
        {
            _network.CreateQueue(address);

            var transport = new InMemTransport(_network, address);

            return new TransportAndInspector(transport, transport);
        }

        public void Dispose()
        {
        }
    }
}