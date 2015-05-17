using NUnit.Framework;
using Rebus.Tests.Contracts.Transports;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture]
    public class AzureServiceBusBasicSendReceive : BasicSendReceive<AzureServiceBusTransportFactory> { }
}