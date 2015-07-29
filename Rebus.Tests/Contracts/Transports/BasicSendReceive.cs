using System;
using System.Collections.Generic;
using System.Linq;
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
            CleanUpDisposables();

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
                await input1.Send(input2QueueName, MessageWith("hej"), context);
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
                await input1.Send(input2QueueName, MessageWith("hej"), context);
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

        [TestCase(true)]
        [TestCase(false)]
        public async Task MultipleSentMessagesCanBeRolledBack(bool commitAndExpectTheMessagesToBeSent)
        {
            var inputQueueName = TestConfig.QueueName("input");
            var input = _factory.Create(inputQueueName);

            await WithContext(async ctx =>
            {
                await input.Send(inputQueueName, MessageWith("hej1"), ctx);
                await input.Send(inputQueueName, MessageWith("hej2"), ctx);
            },
                completeTransaction: commitAndExpectTheMessagesToBeSent);

            var allMessages = await GetAll(input);

            if (commitAndExpectTheMessagesToBeSent)
            {
                Assert.That(allMessages.Count, Is.EqualTo(2));
                Assert.That(allMessages.OrderBy(s => s), Is.EqualTo(new[] { "hej1", "hej2" }));
            }
            else
            {
                Assert.That(allMessages.Count, Is.EqualTo(0));
            }
        }

        async Task<List<string>> GetAll(ITransport input)
        {
            var transportMessages = new List<string>();
            var receivedNulls = 0;

            while (receivedNulls < 5)
            {
                using (var transactionContext = new DefaultTransactionContext())
                {
                    var msg = await input.Receive(transactionContext);

                    if (msg != null)
                    {
                        transportMessages.Add(GetStringBody(msg));
                        await transactionContext.Complete();
                        continue;
                    }

                    await Task.Delay(100);
                    receivedNulls++;
                }
            }

            return transportMessages;
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

            return _defaultEncoding.GetString(transportMessage.Body);
        }

        TransportMessage MessageWith(string stringBody)
        {
            var headers = new Dictionary<string, string>
            {
                {Headers.MessageId, Guid.NewGuid().ToString()}
            };
            var body = _defaultEncoding.GetBytes(stringBody);
            return new TransportMessage(headers, body);
        }
    }
}