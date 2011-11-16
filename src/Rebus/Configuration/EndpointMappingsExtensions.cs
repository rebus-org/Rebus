using Rebus.Configuration.Configurers;

namespace Rebus.Configuration
{
    public static class EndpointMappingsExtensions
    {
         public static void FromNServiceBusConfiguration(this EndpointMappingsConfigurer configurer)
         {
             configurer.Use(new DetermineDestinationFromNServiceBusEndpointMappings(new StandardAppConfigLoader()));
         }
    }
}