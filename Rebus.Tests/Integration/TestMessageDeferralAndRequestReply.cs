using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestMessageDeferralAndRequestReply : FixtureBase
    {
        BuiltinHandlerActivator _service;
        BuiltinHandlerActivator _client;

        protected override void SetUp()
        {
            var inMemNetwork = new InMemNetwork();

            _service = CreateEndpoint(inMemNetwork, "service");
            _client = CreateEndpoint(inMemNetwork, "client");
        }

        BuiltinHandlerActivator CreateEndpoint(InMemNetwork inMemNetwork, string inputQueueName)
        {
            var service = Using(new BuiltinHandlerActivator());

            Configure.With(service)
                .Logging(l => l.Console(minLevel: LogLevel.Warn))
                .Transport(t => t.UseInMemoryTransport(inMemNetwork, inputQueueName))
                .Routing(r => r.TypeBased().Map<string>("service"))
                .Start();

            return service;
        }

        [Test]
        public async Task DeferringRequestDoesNotBreakAbilityToReply()
        {
            var gotReply = new ManualResetEvent(false);

            _service.Handle<string>(async (bus, context, str) =>
            {
                const string deferredMessageHeader = "this message was already deferred";

                if (!context.TransportMessage.Headers.ContainsKey(deferredMessageHeader))
                {
                    var extraHeaders = new Dictionary<string, string> {{deferredMessageHeader, ""}};

                    Console.WriteLine($"SERVICE deferring '{str}' 1 second");
                    await bus.Defer(TimeSpan.FromSeconds(1), str, extraHeaders);
                    return;
                }

                const string reply = "yeehaa!";
                Console.WriteLine($"SERVICE replying '{reply}'");
                await bus.Reply(reply);
            });

            _client.Handle<string>(async reply =>
            {
                Console.WriteLine($"CLIENT got reply '{reply}'");
                gotReply.Set();
            });

            await _client.Bus.Send("request");

            gotReply.WaitOrDie(TimeSpan.FromSeconds(4));
        }
    }
}