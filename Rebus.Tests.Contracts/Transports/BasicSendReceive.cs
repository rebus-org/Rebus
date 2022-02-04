using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Tests.Contracts.Transports;

public abstract class BasicSendReceive<TTransportFactory> : FixtureBase where TTransportFactory : ITransportFactory, new()
{
    readonly Encoding _defaultEncoding = Encoding.UTF8;

    TTransportFactory _factory;
    CancellationToken _cancellationToken;

    protected override void SetUp()
    {
        _cancellationToken = new CancellationTokenSource().Token;
        _factory = new TTransportFactory();
    }

    protected override void TearDown()
    {
        CleanUpDisposables();

        _factory.CleanUp();
    }

    [Test]
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

            Assert.That(transportMessage, Is.Not.Null);

            var stringBody = GetStringBody(transportMessage);

            Assert.That(stringBody, Is.EqualTo("greetings!"));
        });
    }

    [Test]
    public async Task EmptyQueueReturnsNull()
    {
        var emptyQueue = _factory.Create(TestConfig.GetName("empty"));

        await WithContext(async context =>
        {
            var transportMessage = await emptyQueue.Receive(context, _cancellationToken);

            Assert.That(transportMessage, Is.Null);
        });
    }

    [Test]
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

            Assert.That(stringBody, Is.EqualTo("hej"));
        });
    }

    [Test]
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

            Assert.That(transportMessage, Is.Null);
        });
    }

    [Test]
    public async Task MessageIsReturnedToQueueWhenReceivingTransactionIsNotCommitted()
    {
        var input1QueueName = TestConfig.GetName("input1");
        var input2QueueName = TestConfig.GetName("input2");

        var input1 = _factory.Create(input1QueueName);
        var input2 = _factory.Create(input2QueueName);

        Console.WriteLine($"Sending message to {input2QueueName}");

        await WithContext(async context =>
        {
            await input1.Send(input2QueueName, MessageWith("hej"), context);
        });

        Console.WriteLine("Receiving message in tx context, which is rolled back");

        await WithContext(async context =>
        {
            var transportMessage = await input2.Receive(context, _cancellationToken);
            var stringBody = GetStringBody(transportMessage);

            Assert.That(stringBody, Is.EqualTo("hej"));
        }, completeTransaction: false);

        Console.WriteLine("Receiving message in tx context, which is completed");

        await WithContext(async context =>
        {
            var transportMessage = await input2.Receive(context, _cancellationToken);
            var stringBody = GetStringBody(transportMessage);

            Assert.That(stringBody, Is.EqualTo("hej"));
        });

        Console.WriteLine("Receiving message in tx context, expecting null this time");

        await WithContext(async context =>
        {
            var transportMessage = await input2.Receive(context, _cancellationToken);

            Assert.That(transportMessage, Is.Null);
        });
    }

    [TestCase(true)]
    [TestCase(false)]
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
            var receivedMessages = allMessages.OrderBy(s => s).ToArray();
                
            Assert.That(receivedMessages.Count, Is.EqualTo(2), "Two messages were sent, so we expected two messages to be received");
                
            Assert.That(receivedMessages, Is.EqualTo(new[] { "hej1", "hej2" }), 
                $@"Expected that the messages 'hej1' and 'hej2' would have been received, but instead we got this: {string.Join(", ", receivedMessages)}");
        }
        else
        {
            Assert.That(allMessages.Count, Is.EqualTo(0), "The transaction was not completed, so we didn't expect any messages to have been sent");
        }
    }

    async Task<List<string>> GetAll(ITransport input)
    {
        var transportMessages = new List<string>();
        var receivedNulls = 0;

        while (receivedNulls < 5)
        {
            using (var scope = new RebusTransactionScope())
            {
                var msg = await input.Receive(scope.TransactionContext, _cancellationToken);

                if (msg != null)
                {
                    transportMessages.Add(GetStringBody(msg));
                    await scope.CompleteAsync();
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
        using (var scope = new RebusTransactionScope())
        {
            await contextAction(scope.TransactionContext);

            if (completeTransaction)
            {
                await scope.CompleteAsync();
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