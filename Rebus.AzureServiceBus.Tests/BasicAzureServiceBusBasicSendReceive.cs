using NUnit.Framework;
using Rebus.AzureServiceBus.Tests.Factories;
using Rebus.Tests.Contracts.Transports;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture, Category(TestCategory.Azure)]
    public class BasicAzureServiceBusBasicSendReceive : BasicSendReceive<BasicAzureServiceBusTransportFactory> { }
}