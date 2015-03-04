using System.Configuration;
using NUnit.Framework;
using Rebus2.Activation;
using Rebus2.Config;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestConfigurationApi
    {
        [Test]
        public void ThrowsIfNoTransportIsSpecified()
        {
            Assert.Throws<ConfigurationErrorsException>(() => Configure.With(new BuiltinHandlerActivator()).Start());
        }
    }
}