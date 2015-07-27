using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests;
using Rebus.Transport.InMem;

namespace Rebus.XmlConfig.Tests
{
    [TestFixture]
    public class CanReadConfigurationFile : FixtureBase
    {
        [Test]
        public void ItWorks()
        {
            using (AppConfig.Change("Examples/App-01.config"))
            {
                Configure.With(new BuiltinHandlerActivator())
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "bimse"))
                    .Routing(r => r.TypeBasedRoutingFromAppConfig())
                    .Start();
            }
        }

        [Test]
        public void ItWorksWithComplementaryConfigAsWell()
        {
            using (AppConfig.Change("Examples/App-01.config"))
            {
                Configure.With(new BuiltinHandlerActivator())
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "bimse"))
                    .Routing(r => r.TypeBased().AddEndpointMappingsFromAppConfig())
                    .Start();
            }
        }
    }

    public class SomeExistingType { }
}
