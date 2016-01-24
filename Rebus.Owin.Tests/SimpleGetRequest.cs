using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests;
using Rebus.Transport.InMem;

namespace Rebus.Owin.Tests
{
    [TestFixture]
    public class SimpleGetRequest : FixtureBase
    {
        BuiltinHandlerActivator _activator;

        protected override void SetUp()
        {
            _activator = new BuiltinHandlerActivator();

            Configure.With(_activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "doesn't matter"))
                .Options(o =>
                {
                    o.AddWebHost("http://localhost:9090", app =>
                    {

                    });
                })
                .Start();
        }

        [Test]
        public void CanProcessIt()
        {
            
        }
    }
}
