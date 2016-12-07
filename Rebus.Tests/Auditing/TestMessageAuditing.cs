using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Auditing.Messages;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Auditing
{
    public class TestMessageAuditing : FixtureBase
    {
        readonly IBus _bus;
        readonly BuiltinHandlerActivator _adapter;
        readonly InMemNetwork _network;

        public TestMessageAuditing()
        {
            _adapter = new BuiltinHandlerActivator();

            Using(_adapter);

            _network = new InMemNetwork();
            
            _bus = Configure.With(_adapter)
                .Transport(t => t.UseInMemoryTransport(_network, "test"))
                .Options(o =>
                {
                    o.LogPipeline(true);
                    o.EnableMessageAuditing("audit");
                })
                .Start();
        }

        [Fact]
        public async Task DoesNotCopyFailedMessage()
        {
            _adapter.Handle<string>(async _ =>
            {
                throw new Exception("w00t!!");
            });

            await _bus.SendLocal("woohooo!!!!");

            await Task.Delay(TimeSpan.FromSeconds(3));

            var message = _network.GetNextOrNull("audit");

            // If the assert fails, then apparently, a message copy was received anyway!!
            Assert.Null(message);
        }

        [Fact]
        public async Task CopiesProperlyHandledMessageToAuditQueue()
        {
            var gotTheMessage = new ManualResetEvent(false);

            _adapter.Handle<string>(async _ =>
            {
                gotTheMessage.Set();
            });

            await _bus.SendLocal("woohooo!!!!");

            gotTheMessage.WaitOrDie(TimeSpan.FromSeconds(5));

            var message = await _network.WaitForNextMessageFrom("audit");

            PrintHeaders(message);

            Assert.Contains(AuditHeaders.AuditTime, message.Headers.Keys);
            Assert.Contains(AuditHeaders.HandleTime, message.Headers.Keys);
            Assert.Contains(Headers.Intent, message.Headers.Keys);
            Assert.Equal(Headers.IntentOptions.PointToPoint, message.Headers[Headers.Intent]);
        }

        [Fact]
        public async Task CopiesPublishedMessageToAuditQueue()
        {
            await _bus.Advanced.Topics.Publish("TOPIC: 'whocares/nosubscribers'", "woohooo!!!!");

            var message = await _network.WaitForNextMessageFrom("audit");

            PrintHeaders(message);

            Assert.Contains(AuditHeaders.AuditTime, message.Headers.Keys);
            Assert.Contains(Headers.Intent, message.Headers.Keys);
            Assert.Equal(Headers.IntentOptions.PublishSubscribe, message.Headers[Headers.Intent]);
        }

        static void PrintHeaders(TransportMessage message)
        {
            Console.WriteLine(@"Headers:
{0}", string.Join(Environment.NewLine, message.Headers.Select(kvp => $"    {kvp.Key}: {kvp.Value}")));
        }
    }
}