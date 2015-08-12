using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Tests.Extensions;
using Rebus.Transport.Msmq;

namespace Rebus.Tests.Timeouts
{
    [TestFixture]
    public class TestExternalTimeoutManager : FixtureBase
    {
        readonly string _queueName = TestConfig.QueueName("client");
        readonly string _queueNameTimeoutManager = TestConfig.QueueName("manager");

        ManualResetEvent _gotTheMessage;
        IBus _bus;

        protected override void SetUp()
        {
            // start the external timeout manager
            Configure.With(Using(new BuiltinHandlerActivator()))
                .Transport(t => t.UseMsmq(_queueNameTimeoutManager))
                .Start();

            _gotTheMessage = new ManualResetEvent(false);

            // start the client
            var client = Using(new BuiltinHandlerActivator());

            client.Handle<string>(async str => _gotTheMessage.Set());

            Configure.With(client)
                .Transport(t => t.UseMsmq(_queueName))
                .Options(o => o.UseExternalTimeoutManager(_queueNameTimeoutManager))
                .Start();

            _bus = client.Bus;
        }

        [Test]
        public async Task ItWorksEvenThoughDeferredMessageIsAccidentallyReceived()
        {
            var headers = new Dictionary<string, string>
            {
                {Headers.DeferredUntil, DateTimeOffset.Now.Add(TimeSpan.FromSeconds(5)).ToIso8601DateTimeOffset()}
            };

            var stopwatch = Stopwatch.StartNew();

            await _bus.SendLocal("denne besked skal stadig udsættes!", headers);

            _gotTheMessage.WaitOrDie(TimeSpan.FromSeconds(6), "Message was not received within 6,5 seconds (which it should have been since it was only deferred 5 seconds)");

            Assert.That(stopwatch.Elapsed, Is.GreaterThan(TimeSpan.FromSeconds(5)), "It must take more than 5 second to get the message back");
        }

        [Test]
        public async Task ItWorks()
        {
            var stopwatch = Stopwatch.StartNew();

            await _bus.Defer(TimeSpan.FromSeconds(5), "hej med dig min ven!");

            _gotTheMessage.WaitOrDie(TimeSpan.FromSeconds(6), "Message was not received within 6,5 seconds (which it should have been since it was only deferred 5 seconds)");

            Assert.That(stopwatch.Elapsed, Is.GreaterThan(TimeSpan.FromSeconds(5)), "It must take more than 5 second to get the message back");
        }

        protected override void TearDown()
        {
            MsmqUtil.Delete(_queueName);
            MsmqUtil.Delete(_queueNameTimeoutManager);
        }
    }
}