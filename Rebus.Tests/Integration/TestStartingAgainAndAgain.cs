using System;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Compression;
using Rebus.Config;
using Rebus.Encryption;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestStartingAgainAndAgain : FixtureBase
    {
        [Test]
        public void JustDoIt()
        {
            var activator = Using(new BuiltinHandlerActivator());
            var rebusConfigurer = Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "bimse"))
                .Options(o =>
                {
                    o.SetMaxParallelism(1)
                        .SetNumberOfWorkers(1)
                        .EnableCompression();
                });

            rebusConfigurer.Start();

            Assert.Throws<InvalidOperationException>(() =>
            {
                rebusConfigurer.Start();
            });
        }
    }
}