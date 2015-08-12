using NUnit.Framework;
using Rebus.Tests.Contracts.Transports;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture, Category(TestCategory.Azure)]
    public class AzureServiceBusMessageExpiration : MessageExpiration<AzureServiceBusTransportFactory> { }
}