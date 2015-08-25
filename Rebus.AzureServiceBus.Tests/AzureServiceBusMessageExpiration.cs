using NUnit.Framework;
using Rebus.AzureServiceBus.Tests.Factories;
using Rebus.Tests.Contracts.Transports;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture, Category(TestCategory.Azure)]
    public class AzureServiceBusMessageExpiration : MessageExpiration<StandardAzureServiceBusTransportFactory> { }

    [TestFixture, Category(TestCategory.Azure)]
    public class BasicAzureServiceBusMessageExpiration : MessageExpiration<BasicAzureServiceBusTransportFactory> { }
}