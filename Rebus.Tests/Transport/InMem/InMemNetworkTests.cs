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

        public ITransport Create(string inputQueueAddress)
        {
            return new InMemTransport(_network, inputQueueAddress);
        }

        public void CleanUp()
        {
        }
    }
}