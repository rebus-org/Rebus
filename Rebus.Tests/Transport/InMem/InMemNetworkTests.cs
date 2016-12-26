using NUnit.Framework;
using Rebus.Tests.Contracts.Transports;
using Rebus.Transport;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Transport.InMem
{
    [TestFixture]
    public class InMemTransportBasicSendReceive : BasicSendReceive<InMemTransportFactory> { }

    [TestFixture]
    public class InMemTransportMessageExpiration : MessageExpiration<InMemTransportFactory> { }

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
}