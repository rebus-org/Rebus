using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Tests.Contracts.Transports
{
    public class BasicSendReceive<TTransportFactory> : FixtureBase where TTransportFactory : ITransportFactory, new()
    {
        readonly Encoding _defaultEncoding = Encoding.UTF8;

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
        public async Task EmptyQueueReturnsNull()
        {
            var emptyQueue = _factory.Create(TestConfig.QueueName("empty"));

            await WithContext(async context =>
            {
                var transportMessage = await emptyQueue.Receive(context);

                Assert.That(transportMessage, Is.Null);
            });
        }

        [Test]
        public async Task CanSendAndReceive()
        {
            var input1QueueName = TestConfig.QueueName("input1");
            var input2QueueName = TestConfig.QueueName("input2");
            
            var input1 = _factory.Create(input1QueueName);
            var input2 = _factory.Create(input2QueueName);

            await WithContext(async context =>
            {
                await input1.Send(input2QueueName, MessageWith("hej"), context);
            });

            await WithContext(async context =>
            {
                var transportMessage = await input2.Receive(context);
                var stringBody = GetStringBody(transportMessage);

                Assert.That(stringBody, Is.EqualTo("hej"));
            });
        }

        [Test]
        public async Task MessageIsNotSentWhenTransactionIsNotCompleted()
        {
            var input1QueueName = TestConfig.QueueName("input1");
            var input2QueueName = TestConfig.QueueName("input2");

            var input1 = _factory.Create(input1QueueName);
            var input2 = _factory.Create(input2QueueName);

            await WithContext(async context =>
            {
                await input1.Send("input2", MessageWith("hej"), context);
            }, 
            completeTransaction: false);

            await WithContext(async context =>
            {
                var transportMessage = await input2.Receive(context);

                Assert.That(transportMessage, Is.Null);
            });
        }

        [Test]
        public async Task MessageIsReturnedToQueueWhenReceivingTransactionIsNotCommitted()
        {
            var input1QueueName = TestConfig.QueueName("input1");
            var input2QueueName = TestConfig.QueueName("input2");

            var input1 = _factory.Create(input1QueueName);
            var input2 = _factory.Create(input2QueueName);

            await WithContext(async context =>
            {
                await input1.Send("input2", MessageWith("hej"), context);
            });

            await WithContext(async context =>
            {
                var transportMessage = await input2.Receive(context);
                var stringBody = GetStringBody(transportMessage);

                Assert.That(stringBody, Is.EqualTo("hej"));
            }, completeTransaction: false);

            await WithContext(async context =>
            {
                var transportMessage = await input2.Receive(context);
                var stringBody = GetStringBody(transportMessage);

                Assert.That(stringBody, Is.EqualTo("hej"));
            });

            await WithContext(async context =>
            {
                var transportMessage = await input2.Receive(context);

                Assert.That(transportMessage, Is.Null);
            });
        }

        async Task WithContext(Func<ITransactionContext, Task> contextAction, bool completeTransaction = true)
        {
            using (var context = new DefaultTransactionContext())
            {
                await contextAction(context);

                if (completeTransaction)
                {
                    await context.Complete();
                }
            }
        }

        string GetStringBody(TransportMessage transportMessage)
        {
            if (transportMessage == null)
            {
                throw new InvalidOperationException("Cannot get string body out of null message!");
            }

            using (var reader = new StreamReader(transportMessage.Body, _defaultEncoding))
            {
                return reader.ReadToEnd();
            }
        }

        TransportMessage MessageWith(string stringBody)
        {
            var headers = new Dictionary<string, string>
            {
                {Headers.MessageId, Guid.NewGuid().ToString()}
            };
            var body = new MemoryStream(_defaultEncoding.GetBytes(stringBody));
            return new TransportMessage(headers, body);
        }
    }
}