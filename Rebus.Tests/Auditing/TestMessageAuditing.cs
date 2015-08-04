using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Auditing;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Auditing
{
    [TestFixture]
    public class TestMessageAuditing : FixtureBase
    {
        IBus _bus;
        BuiltinHandlerActivator _adapter;
        InMemNetwork _network;

        protected override void SetUp()
        {
            _adapter = new BuiltinHandlerActivator();
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

        [Test]
        public async Task DoesNotCopyFailedMessage()
        {
            _adapter.Handle<string>(async _ =>
            {
                throw new Exception("w00t!!");
            });

            await _bus.SendLocal("woohooo!!!!");

            await Task.Delay(TimeSpan.FromSeconds(3));

            var message = _network.GetNextOrNull("audit");

            Assert.That(message, Is.Null, "Apparently, a message copy was received anyway!!");
        }

        [Test]
        public async Task CopiesProperlyHandledMessageToAuditQueue()
        {
            var gotTheMessage = new ManualResetEvent(false);

            _adapter.Handle<string>(async _ =>
            {
                gotTheMessage.Set();
            });

            await _bus.SendLocal("woohooo!!!!");

            gotTheMessage.WaitOrDie(TimeSpan.FromSeconds(5));

            InMemTransportMessage message;
            var timer = Stopwatch.StartNew();

            while ((message = _network.GetNextOrNull("audit")) == null)
            {
                await Task.Delay(200);

                if (timer.Elapsed > TimeSpan.FromSeconds(2))
                {
                    Assert.Fail("Did not receive message copy within 2 seconds of waiting....");
                }
            }

            PrintHeaders(message);

            Assert.That(message.Headers.ContainsKey(AuditHeaders.AuditTime));
            Assert.That(message.Headers.ContainsKey(AuditHeaders.HandleTime));
            Assert.That(message.Headers.ContainsKey(Headers.Intent));
            Assert.That(message.Headers[Headers.Intent], Is.EqualTo(Headers.IntentOptions.PointToPoint));
        }

        static void PrintHeaders(InMemTransportMessage message)
        {
            Console.WriteLine(@"Headers:
{0}", string.Join(Environment.NewLine, message.Headers.Select(kvp => string.Format("    {0}: {1}", kvp.Key, kvp.Value))));
        }

        [Test]
        public async Task CopiesPublishedMessageToAuditQueue()
        {
            await _bus.Publish("TOPIC: 'whocares/nosubscribers'", "woohooo!!!!");

            InMemTransportMessage message;
            var timer = Stopwatch.StartNew();

            while ((message = _network.GetNextOrNull("audit")) == null)
            {
                await Task.Delay(200);

                if (timer.Elapsed > TimeSpan.FromSeconds(2))
                {
                    Assert.Fail("Did not receive message copy within 2 seconds of waiting....");
                }
            }

            PrintHeaders(message);

            Assert.That(message.Headers.ContainsKey(AuditHeaders.AuditTime));
            Assert.That(message.Headers.ContainsKey(Headers.Intent));
            Assert.That(message.Headers[Headers.Intent], Is.EqualTo(Headers.IntentOptions.PublishSubscribe));
        }
    }
}