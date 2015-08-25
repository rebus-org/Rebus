using Rebus.AzureServiceBus.Config;

namespace Rebus.AzureServiceBus.Tests.Factories
{
    public class BasicAzureServiceBusBusFactory : AzureServiceBusBusFactory
    {
        public BasicAzureServiceBusBusFactory()
            : base(AzureServiceBusMode.Basic)
        {
        }
    }
}