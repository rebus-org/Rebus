using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests;
using Rebus.Tests.Extensions;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture, Category(TestCategory.Azure)]
    public class NativeDeferTest : FixtureBase
    {
        static readonly string QueueName = TestConfig.QueueName("input");
        BuiltinHandlerActivator _activator;
        IBus _bus;

        protected override void SetUp()
        {
            new AzureServiceBusTransport(AzureServiceBusTransportFactory.ConnectionString, QueueName).PurgeInputQueue();

            _activator = new BuiltinHandlerActivator();

            _bus = Configure.With(_activator)
                .Transport(t => t.UseAzureServiceBus(AzureServiceBusTransportFactory.ConnectionString, QueueName))
                .Options(o =>
                {
                    o.LogPipeline();
                })
                .Start();

            Using(_bus);
        }

        [Test]
        public async Task UsesNativeDeferraltMechanism()
        {
            var done = new ManualResetEvent(false);
            var receiveTime = DateTimeOffset.MinValue;

            _activator.Handle<TimedMessage>(async message =>
            {
                receiveTime = DateTimeOffset.Now;
                done.Set();
            });

            var sendTime = DateTimeOffset.Now;

            await _bus.Defer(TimeSpan.FromSeconds(5), new TimedMessage { Time = sendTime });

            done.WaitOrDie(TimeSpan.FromSeconds(8), "Did not receive 5s-deferred message within 8 seconds of waiting....");

            var delay = receiveTime - sendTime;

            Console.WriteLine("Message was delayed {0}", delay);

            Assert.That(delay, Is.GreaterThan(TimeSpan.FromSeconds(5)), "The message not delayed at least 5 seconds as expected!");
        }

        class TimedMessage
        {
            public DateTimeOffset Time { get; set; }
        }
    }
}