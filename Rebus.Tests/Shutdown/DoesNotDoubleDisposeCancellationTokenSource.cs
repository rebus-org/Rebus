using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Shutdown
{
    [TestFixture]
    public class DoesNotDoubleDisposeCancellationTokenSource : FixtureBase
    {
        [Test]
        public void WhatTheFixtureSays()
        {
            using (var activator = new BuiltinHandlerActivator())
            {
                Configure.With(activator)
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "double-disposal-nono"))
                    .Start();


            }
        }

        [Test]
        public void WhatTheFixtureSays_OneWay()
        {
            using (var activator = new BuiltinHandlerActivator())
            {
                Configure.With(activator)
                    .Transport(t => t.UseInMemoryTransportAsOneWayClient(new InMemNetwork()))
                    .Start();


            }
        }
    }
}