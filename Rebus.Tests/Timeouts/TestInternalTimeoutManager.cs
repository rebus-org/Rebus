using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Extensions;
using Rebus.Transport.Msmq;

namespace Rebus.Tests.Timeouts
{
    [TestFixture]
    public class TestInternalTimeoutManager : FixtureBase
    {
        readonly string _queueName = TestConfig.QueueName("timeouts");

        [Test]
        public async Task WorksOutOfTheBoxWithInternalTimeoutManager()
        {
            var gotTheMessage = new ManualResetEvent(false);
            var activator = new BuiltinHandlerActivator();

            activator.Handle<string>(async str => gotTheMessage.Set());

            Configure.With(activator)
                .Transport(t => t.UseMsmq(_queueName))
                .Start();

            var stopwatch = Stopwatch.StartNew();

            await activator.Bus.Defer(TimeSpan.FromSeconds(5), "hej med dig min ven!");

            gotTheMessage.WaitOrDie(TimeSpan.FromSeconds(6), "Message was not received within 6,5 seconds (which it should have been since it was only deferred 5 seconds)");
            
            Assert.That(stopwatch.Elapsed, Is.GreaterThan(TimeSpan.FromSeconds(5)), "It must take more than 5 second to get the message back");
        }

        protected override void TearDown()
        {
            MsmqUtil.Delete(_queueName);
        }
    }
}