using Rebus.AzureServiceBus.Config;

namespace Rebus.AzureServiceBus.Tests.Factories
{
    public class StandardAzureServiceBusBusFactory : AzureServiceBusBusFactory
    {
        public StandardAzureServiceBusBusFactory()
            : base(AzureServiceBusMode.Standard)
        {
        }
    }
}