using NUnit.Framework;
using Rebus.AzureServiceBus.Tests.Factories;
using Rebus.Tests.Contracts.Transports;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture, Category(TestCategory.Azure)]
    public class AzureServiceBusManyMessages : TestManyMessages<StandardAzureServiceBusBusFactory> { }

    [TestFixture, Category(TestCategory.Azure)]
    public class BasicAzureServiceBusManyMessages : TestManyMessages<BasicAzureServiceBusBusFactory> { }
}