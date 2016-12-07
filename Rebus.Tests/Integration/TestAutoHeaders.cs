using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Integration
{
    public class TestAutoHeaders : FixtureBase
    {
        readonly BuiltinHandlerActivator _activator;
        readonly IBus _bus;

        public TestAutoHeaders()
        {
            _activator = Using(new BuiltinHandlerActivator());

            _bus = Configure.With(_activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "headers"))
                .Start();
        }

        [Fact]
        public async Task AutomaticallyAssignsHeadersFromAttribute()
        {
            var headers = await GetHeaders(new SomeMessage());

            AssertHeader(headers, "greeting", "hej");
        }

        [Fact]
        public async Task DoesNotOverwriteExistingHeaders()
        {
            var headers = await GetHeaders(new SomeMessage(), new Dictionary<string, string>
            {
                {"greeting", "customizzle!"}
            });

            AssertHeader(headers, "greeting", "customizzle!");
        }

        [Header("greeting", "hej")]
        class SomeMessage
        {

        }

        [Fact]
        public async Task WorksWithCustomizedPrefabHeader()
        {
            var headers = await GetHeaders(new MessageWithPrefabHeader());

            AssertHeader(headers, "PREFAB", "");
        }

        [PrefabHeader]
        class MessageWithPrefabHeader { }

        class PrefabHeaderAttribute : HeaderAttribute
        {
            public PrefabHeaderAttribute()
                : base("PREFAB", "")
            {
            }
        }

        async Task<Dictionary<string, string>> GetHeaders(object message, Dictionary<string, string> optionalHeaders = null)
        {
            Dictionary<string, string> headers = null;
            var gotIt = new ManualResetEvent(false);
        
            _activator.Handle<object>(async (bus, context, msg) =>
            {
                headers = context.TransportMessage.Headers;
                gotIt.Set();
            });

            await _bus.SendLocal(message, optionalHeaders);

            gotIt.WaitOrDie(TimeSpan.FromSeconds(1));

            return headers;
        }

        static void AssertHeader(IReadOnlyDictionary<string, string> headers, string expectedKey, string expectedValue)
        {
            Assert.NotNull(headers);
            Assert.True(headers.ContainsKey(expectedKey), $"Headers did not have the '{expectedKey}' key");
            Assert.Equal(expectedValue, headers[expectedKey]);
        }
    }
}