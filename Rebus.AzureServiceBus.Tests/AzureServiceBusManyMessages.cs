using NUnit.Framework;
using Rebus.Tests.Integration.ManyMessages;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture, Category(TestCategory.Azure)]
    public class AzureServiceBusManyMessages : TestManyMessages<AzureServiceBusBusFactory>
    {
    }
}