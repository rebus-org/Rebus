using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Retry.Simple;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Extensions;
using Rebus.Transport.Msmq;

namespace Rebus.Tests.Integration
{
    [TestFixture, Category(Categories.Msmq)]
    public class TestRetry : FixtureBase
    {
        static readonly string InputQueueName = TestConfig.QueueName(string.Format("test.rebus2.retries.input@{0}", Environment.MachineName));
        static readonly string ErrorQueueName = TestConfig.QueueName("rebus2.error");

        BuiltinHandlerActivator _handlerActivator;
        IBus _bus;

        void InitializeBus(int numberOfRetries)
        {
            _handlerActivator = new BuiltinHandlerActivator();

            _bus = Configure.With(_handlerActivator)
                .Logging(l => l.Console(minLevel: LogLevel.Warn))
                .Transport(t => t.UseMsmq(InputQueueName))
                .Routing(r => r.TypeBased().Map<string>(InputQueueName))
                .Options(o => o.SimpleRetryStrategy(maxDeliveryAttempts: numberOfRetries, errorQueueAddress: ErrorQueueName))
                .Start();

            Using(_bus);
        }

        protected override void TearDown()
        {
            MsmqUtil.Delete(InputQueueName);
            MsmqUtil.Delete(ErrorQueueName);
        }

        [Test]
        public async Task ItWorks()
        {
            const int numberOfRetries = 5;

            InitializeBus(numberOfRetries);

            var attemptedDeliveries = 0;

            _handlerActivator.Handle<string>(async _ =>
            {
                Interlocked.Increment(ref attemptedDeliveries);
                throw new ApplicationException("omgwtf!");
            });

            await _bus.Send("hej");

            using (var errorQueue = new MsmqTransport(ErrorQueueName))
            {
                var failedMessage = await errorQueue.AwaitReceive();

                Assert.That(attemptedDeliveries, Is.EqualTo(numberOfRetries));
                Assert.That(failedMessage.Headers.GetValue(Headers.ErrorDetails), Contains.Substring("5 unhandled exceptions"));
                Assert.That(failedMessage.Headers.GetValue(Headers.SourceQueue), Is.EqualTo(InputQueueName));
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(5)]
        [TestCase(40)]
        [TestCase(70)]
        [TestCase(90)]
        public async Task CanConfigureNumberOfRetries(int numberOfRetries)
        {
            InitializeBus(numberOfRetries);

            var attemptedDeliveries = 0;

            _handlerActivator.Handle<string>(async _ =>
            {
                Interlocked.Increment(ref attemptedDeliveries);
                throw new ApplicationException("omgwtf!");
            });

            await _bus.Send("hej");

            using (var errorQueue = new MsmqTransport(ErrorQueueName))
            {
                var expectedNumberOfAttemptedDeliveries = numberOfRetries;

                await errorQueue.AwaitReceive(2 + numberOfRetries / 10.0);

                Assert.That(attemptedDeliveries, Is.EqualTo(expectedNumberOfAttemptedDeliveries));
            }
        }
    }
}