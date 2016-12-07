using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Transport;
using Xunit;

namespace Rebus.Tests.Contracts.Transports
{
    public abstract class MessageExpiration<TTransportFactory> : FixtureBase where TTransportFactory : ITransportFactory, new()
    {
        TTransportFactory _factory;
        CancellationToken _cancellationToken;

        protected MessageExpiration()
        {
            _factory = new TTransportFactory();
            _cancellationToken = new CancellationTokenSource().Token;
        }

        protected override void TearDown()
        {
            _factory.CleanUp();
        }

        [Fact]
        public async Task ReceivesNonExpiredMessage()
        {
            var queueName = TestConfig.GetName("expiration");
            var transport = _factory.Create(queueName);
            var id = Guid.NewGuid().ToString();

            using (var transactionContext = new DefaultTransactionContext())
            {
                var headers = new Dictionary<string, string>
                {
                    {Headers.MessageId, Guid.NewGuid().ToString() },
                    {"recognizzle", id}
                };
                await transport.Send(queueName, MessageWith(headers), transactionContext);
                await transactionContext.Complete();
            }

            await Task.Delay(5000);

            using (var transactionContext = new DefaultTransactionContext())
            {
                var transportMessage = await transport.Receive(transactionContext, _cancellationToken);
                await transactionContext.Complete();

                Assert.NotNull(transportMessage);

                var headers = transportMessage.Headers;

                Assert.Contains("recognizzle", headers.Keys);
                Assert.Equal(id, headers["recognizzle"]);
            }
        }

        [Fact]
        public async Task DoesNotReceiveExpiredMessage()
        {
            var queueName = TestConfig.GetName("expiration");
            var transport = _factory.Create(queueName);
            var id = Guid.NewGuid().ToString();

            using (var transactionContext = new DefaultTransactionContext())
            {
                var headers = new Dictionary<string, string>
                {
                    {Headers.MessageId, Guid.NewGuid().ToString() },
                    {"recognizzle", id},
                    {Headers.TimeToBeReceived, "00:00:04"} //< expires after 4 seconds!
                };
                await transport.Send(queueName, MessageWith(headers), transactionContext);
                await transactionContext.Complete();
            }

            const int millisecondsDelay = 7000;

            var stopwatch = Stopwatch.StartNew();
            await Task.Delay(millisecondsDelay);
            Console.WriteLine($"Delay of {millisecondsDelay} ms actually lasted {stopwatch.ElapsedMilliseconds:0} ms");

            using (var transactionContext = new DefaultTransactionContext())
            {
                var transportMessage = await transport.Receive(transactionContext, _cancellationToken);
                await transactionContext.Complete();

                Assert.Null(transportMessage);
            }
        }

        [Fact]
        public async Task ReceivesAlmostExpiredMessage()
        {
            var queueName = TestConfig.GetName("expiration");
            var transport = _factory.Create(queueName);
            var id = Guid.NewGuid().ToString();

            using (var transactionContext = new DefaultTransactionContext())
            {
                var headers = new Dictionary<string, string>
                {
                    {Headers.MessageId, Guid.NewGuid().ToString() },
                    {"recognizzle", id},
                    {Headers.TimeToBeReceived, "00:00:20"},
                    {Headers.SentTime,DateTimeOffset.UtcNow.ToString("O")}//< expires after 10 seconds!
                };
                await transport.Send(queueName, MessageWith(headers), transactionContext);
                await transactionContext.Complete();
            }

            await Task.Delay(3000);

            using (var transactionContext = new DefaultTransactionContext())
            {
                var transportMessage = await transport.Receive(transactionContext, _cancellationToken);
                await transactionContext.Complete();

                Assert.NotNull(transportMessage);
            }
        }

        static TransportMessage MessageWith(Dictionary<string, string> headers)
        {
            return new TransportMessage(headers, DontCareAboutTheBody());
        }

        static byte[] DontCareAboutTheBody()
        {
            return System.Text.Encoding.UTF8.GetBytes("Dont Care About The Body");
        }
    }
}