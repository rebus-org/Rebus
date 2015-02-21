using NUnit.Framework;
using Rebus2.Activation;
using Rebus2.Config;

namespace Tests.Integration
{
    [TestFixture]
    public class TestConfigurationApi
    {
        [Test]
        public void ThrowsIfNoTransportIsSpecified()
        {
            Configure.With(new BuiltinHandlerActivator()).Start();
        }
    }
}