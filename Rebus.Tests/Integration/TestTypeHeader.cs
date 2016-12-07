using System.Collections.Generic;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Integration
{
    public class TestTypeHeader : FixtureBase
    {
        [Fact]
        public void SentMessageCarriesTheTypeHeader()
        {
            var activator = Using(new BuiltinHandlerActivator());

            var receivedHeaders = new Dictionary<string, string>();
            var counter = new SharedCounter(1);

            activator.Handle<SomeMessage>(async (bus, context, message) =>
            {
                foreach (var kvp in context.Headers)
                {
                    receivedHeaders.Add(kvp.Key, kvp.Value);
                }
                counter.Decrement();
            });

            Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "type-header-test"))
                .Options(o => o.LogPipeline())
                .Start();

            activator.Bus.SendLocal(new SomeMessage()).Wait();

            counter.WaitForResetEvent();

            Assert.True(receivedHeaders.ContainsKey(Headers.Type));
            Assert.Equal("Rebus.Tests.Integration.SomeMessage, Rebus.Tests", receivedHeaders[Headers.Type]);
        }
    }

    class SomeMessage { }
}