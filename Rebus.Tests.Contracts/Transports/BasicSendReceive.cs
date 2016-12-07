using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Transport;
using Xunit;

namespace Rebus.Tests.Contracts.Transports
{
    public class BasicSendReceive<TTransportFactory> : FixtureBase where TTransportFactory : ITransportFactory, new()
    {
        readonly Encoding _defaultEncoding = Encoding.UTF8;

        TTransportFactory _factory;
        CancellationToken _cancellationToken;

        protected BasicSendReceive()
        {
            _cancellationToken = new CancellationTokenSource().Token;
            _factory = new TTransportFactory();
        }

        protected override void TearDown()
        {
            CleanUpDisposables();

            _factory.CleanUp();
        }

        [Fact]
        public async Task HasOneWayClient()
        {
            var receiverQueue = TestConfig.GetName("receiver");
            
            var client = _factory.CreateOneWayClient();
            var receiver = _factory.Create(receiverQueue);

            await WithContext(async context =>
            {
                await client.Send(receiverQueue, MessageWith("greetings!"), context);
            });

            await WithContext(async context =>
            {
                var transportMessage = await receiver.Receive(context, _cancellationToken);

                Assert.NotNull(transportMessage);

                var stringBody = GetStringBody(transportMessage);

                Assert.Equal("greetings!", stringBody);
            });
        }

        [Fact]
        public async Task EmptyQueueReturnsNull()
        {
            var emptyQueue = _factory.Create(TestConfig.GetName("empty"));

            await WithContext(async context =>
            {
                var transportMessage = await emptyQueue.Receive(context, _cancellationToken);

                Assert.Null(transportMessage);
            });
        }

        [Fact]
        public async Task CanSendAndReceive()
        {
            var input1QueueName = TestConfig.GetName("input1");
            var input2QueueName = TestConfig.GetName("input2");

            var input1 = _factory.Create(input1QueueName);
            var input2 = _factory.Create(input2QueueName);

            await WithContext(async context =>
            {
                await input1.Send(input2QueueName, MessageWith("hej"), context);
            });

            await WithContext(async context =>
            {
                var transportMessage = await input2.Receive(context, _cancellationToken);
                var stringBody = GetStringBody(transportMessage);

                Assert.Equal("hej", stringBody);
            });
        }

        [Fact]
        public async Task MessageIsNotSentWhenTransactionIsNotCompleted()
        {
            var input1QueueName = TestConfig.GetName("input1");
            var input2QueueName = TestConfig.GetName("input2");

            var input1 = _factory.Create(input1QueueName);
            var input2 = _factory.Create(input2QueueName);

            await WithContext(async context =>
            {
                await input1.Send(input2QueueName, MessageWith("hej"), context);
            },
            completeTransaction: false);

            await WithContext(async context =>
            {
                var transportMessage = await input2.Receive(context, _cancellationToken);

                Assert.Null(transportMessage);
            });
        }

        [Fact]
        public async Task MessageIsReturnedToQueueWhenReceivingTransactionIsNotCommitted()
        {
            var input1QueueName = TestConfig.GetName("input1");
            var input2QueueName = TestConfig.GetName("input2");

            var input1 = _factory.Create(input1QueueName);
            var input2 = _factory.Create(input2QueueName);

            await WithContext(async context =>
            {
                await input1.Send(input2QueueName, MessageWith("hej"), context);
            });

            await WithContext(async context =>
            {
                var transportMessage = await input2.Receive(context, _cancellationToken);
                var stringBody = GetStringBody(transportMessage);

                Assert.Equal("hej", stringBody);
            }, completeTransaction: false);

            await WithContext(async context =>
            {
                var transportMessage = await input2.Receive(context, _cancellationToken);
                var stringBody = GetStringBody(transportMessage);

                Assert.Equal("hej", stringBody);
            });

            await WithContext(async context =>
            {
                var transportMessage = await input2.Receive(context, _cancellationToken);

                Assert.Null(transportMessage);
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task MultipleSentMessagesCanBeRolledBack(bool commitAndExpectTheMessagesToBeSent)
        {
            var inputQueueName = TestConfig.GetName("input");
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
                Assert.Equal(2, allMessages.Count);
                Assert.Equal(new[] { "hej1", "hej2" }, allMessages.OrderBy(s => s));
            }
            else
            {
                Assert.Equal(0, allMessages.Count);
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
                    var msg = await input.Receive(transactionContext, _cancellationToken);

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