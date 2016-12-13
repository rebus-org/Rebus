using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Retry.Simple;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Integration
{
    public class TestRetry : FixtureBase
    {
        private static readonly string InputQueueName = TestConfig.GetName($"test.rebus2.retries.input@{Environment.MachineName}");
        public static readonly string ErrorQueueName = TestConfig.GetName("rebus2.error");

        private BuiltinHandlerActivator _handlerActivator;
        private IBus _bus;
        private InMemNetwork _network;

        void InitializeBus(int numberOfRetries)
        {
            _network = new InMemNetwork();

            _handlerActivator = new BuiltinHandlerActivator();

            _bus = Configure.With(_handlerActivator)
                .Logging(l => l.Console(minLevel: LogLevel.Warn))
                .Transport(t => t.UseInMemoryTransport(_network, InputQueueName))
                .Routing(r => r.TypeBased().Map<string>(InputQueueName))
                .Options(o => o.SimpleRetryStrategy(maxDeliveryAttempts: numberOfRetries, errorQueueAddress: ErrorQueueName))
                .Start();

            Using(_bus);
        }

        [Fact]
        public async Task ItWorks()
        {
            const int numberOfRetries = 5;

            InitializeBus(numberOfRetries);

            var attemptedDeliveries = 0;

            _handlerActivator.Handle<string>(async _ =>
            {
                Interlocked.Increment(ref attemptedDeliveries);
                throw new Exception("omgwtf!");
            });

            await _bus.Send("hej");

            var failedMessage = await _network.WaitForNextMessageFrom(ErrorQueueName);

            Assert.Equal(numberOfRetries, attemptedDeliveries);
            Assert.Contains($"{numberOfRetries} unhandled exceptions", failedMessage.Headers.GetValue(Headers.ErrorDetails));
            Assert.Equal(InputQueueName, failedMessage.Headers.GetValue(Headers.SourceQueue));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(5)]
        public async Task CanConfigureNumberOfRetries(int numberOfRetries)
        {
            InitializeBus(numberOfRetries);

            var attemptedDeliveries = 0;

            _handlerActivator.Handle<string>(async _ =>
            {
                Interlocked.Increment(ref attemptedDeliveries);
                throw new Exception("omgwtf!");
            });

            await _bus.Send("hej");

            await _network.WaitForNextMessageFrom(ErrorQueueName);

            var expectedNumberOfAttemptedDeliveries = numberOfRetries;

            Assert.Equal(expectedNumberOfAttemptedDeliveries, attemptedDeliveries);
        }
    }
}