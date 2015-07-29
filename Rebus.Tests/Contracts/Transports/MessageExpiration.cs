using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Tests.Contracts.Transports
{
    public class MessageExpiration<TTransportFactory> : FixtureBase where TTransportFactory : ITransportFactory, new()
    {
        TTransportFactory _factory;

        protected override void SetUp()
        {
            _factory = new TTransportFactory();
        }

        protected override void TearDown()
        {
            _factory.CleanUp();
        }

        [Test]
        public async Task ReceivesNonExpiredMessage()
        {
            var queueName = TestConfig.QueueName("expiration");
            var transport = _factory.Create(queueName);
            var id = Guid.NewGuid().ToString();

            using (var transactionContext = new DefaultTransactionContext())
            {
                var headers = new Dictionary<string, string>
                {
                    {"recognizzle", id}
                };
                await transport.Send(queueName, MessageWith(headers), transactionContext);
                await transactionContext.Complete();
            }

            await Task.Delay(5000);

            using (var transactionContext = new DefaultTransactionContext())
            {
                var transportMessage = await transport.Receive(transactionContext);
                await transactionContext.Complete();

                Assert.That(transportMessage, Is.Not.Null);

                var headers = transportMessage.Headers;

                Assert.That(headers.ContainsKey("recognizzle"));
                Assert.That(headers["recognizzle"], Is.EqualTo(id));
            }
        }

        [Test]
        public async Task DoesNotReceiveExpiredMessage()
        {
            var queueName = TestConfig.QueueName("expiration");
            var transport = _factory.Create(queueName);
            var id = Guid.NewGuid().ToString();

            using (var transactionContext = new DefaultTransactionContext())
            {
                var headers = new Dictionary<string, string>
                {
                    {"recognizzle", id},
                    {Headers.TimeToBeReceived, "00:00:04"} //< expires after 4 seconds!
                };
                await transport.Send(queueName, MessageWith(headers), transactionContext);
                await transactionContext.Complete();
            }

            await Task.Delay(5000);

            using (var transactionContext = new DefaultTransactionContext())
            {
                var transportMessage = await transport.Receive(transactionContext);
                await transactionContext.Complete();

                Assert.That(transportMessage, Is.Null);
            }
        }

        [Test]
        public async Task ReceivesAlmostExpiredMessage()
        {
            var queueName = TestConfig.QueueName("expiration");
            var transport = _factory.Create(queueName);
            var id = Guid.NewGuid().ToString();

            using (var transactionContext = new DefaultTransactionContext())
            {
                var headers = new Dictionary<string, string>
                {
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
                var transportMessage = await transport.Receive(transactionContext);
                await transactionContext.Complete();

                Assert.That(transportMessage, Is.Not.Null);
            }
        }

        static TransportMessage MessageWith(Dictionary<string, string> headers)
        {
            return new TransportMessage(headers, DontCareAboutTheBody());
        }

        static byte[] DontCareAboutTheBody()
        {
            return new byte[] { 1, 2, 3 };
        }
    }
}