using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Persistence.SqlServer;
using Rebus.Transport;
using Rebus.Transport.SqlServer;
using Timer = System.Timers.Timer;

namespace Rebus.Tests.Transport.SqlServer
{
    [TestFixture, Category(Categories.SqlServer)]
    public class TestSqlServerTransport : FixtureBase
    {
        const string QueueName = "input";
        readonly string _tableName = "messages" + TestConfig.Suffix;
        SqlServerTransport _transport;

        protected override void SetUp()
        {
            SqlTestHelper.DropTable(_tableName);

            _transport = new SqlServerTransport(new DbConnectionProvider(SqlTestHelper.ConnectionString), _tableName, QueueName);
            _transport.EnsureTableIsCreated();

            Using(_transport);

            _transport.Initialize();
        }

        [Test]
        public async Task ReceivesSentMessageWhenTransactionIsCommitted()
        {
            using (var context = new DefaultTransactionContext())
            {
                await _transport.Send(QueueName, RecognizableMessage(), context);

                await context.Complete();
            }

            using (var context = new DefaultTransactionContext())
            {
                var transportMessage = await _transport.Receive(context);

                await context.Complete();

                AssertMessageIsRecognized(transportMessage);
            }
        }

        [Test]
        public async Task DoesNotReceiveSentMessageWhenTransactionIsNotCommitted()
        {
            using (var context = new DefaultTransactionContext())
            {
                await _transport.Send(QueueName, RecognizableMessage(), context);

                //await context.Complete();
            }

            using (var context = new DefaultTransactionContext())
            {
                var transportMessage = await _transport.Receive(context);

                Assert.That(transportMessage, Is.Null);
            }
        }

        [TestCase(1000)]
        public async Task LotsOfAsyncStuffGoingDown(int numberOfMessages)
        {
            var receivedMessages = 0;
            var messageIds = new ConcurrentDictionary<int, int>();

            Console.WriteLine("Sending {0} messages", numberOfMessages);

            await Task.WhenAll(Enumerable.Range(0, numberOfMessages)
                .Select(async i =>
                {
                    using (var context = new DefaultTransactionContext())
                    {
                        await _transport.Send(QueueName, RecognizableMessage(i), context);
                        await context.Complete();

                        messageIds[i] = 0;
                    }
                }));

            Console.WriteLine("Receiving {0} messages", numberOfMessages);

            using (var timer = new Timer(1000))
            {
                timer.Elapsed += delegate
                {
                    Console.WriteLine("Received: {0} msgs", receivedMessages);
                };
                timer.Start();

                await Task.WhenAll(Enumerable.Range(0, numberOfMessages)
                    .Select(async i =>
                    {
                        using (var context = new DefaultTransactionContext())
                        {
                            var msg = await _transport.Receive(context);
                            await context.Complete();

                            Interlocked.Increment(ref receivedMessages);

                            var id = int.Parse(msg.Headers["id"]);

                            messageIds.AddOrUpdate(id, 1, (_, existing) => existing + 1);
                        }
                    }));

                await Task.Delay(1000);
            }

            Assert.That(messageIds.Keys.OrderBy(k => k).ToArray(), Is.EqualTo(Enumerable.Range(0, numberOfMessages).ToArray()));

            var kvpsDifferentThanOne = messageIds.Where(kvp => kvp.Value != 1).ToList();

            if (kvpsDifferentThanOne.Any())
            {
                Assert.Fail(@"Oh no! the following IDs were not received exactly once:

{0}",
    string.Join(Environment.NewLine, kvpsDifferentThanOne.Select(kvp => string.Format("   {0}: {1}", kvp.Key, kvp.Value))));
            }
        }

        void AssertMessageIsRecognized(TransportMessage transportMessage)
        {
            Assert.That(transportMessage.Headers.GetValue("recognizzle"), Is.EqualTo("hej"));
        }

        TransportMessage RecognizableMessage(int id = 0)
        {
            var headers = new Dictionary<string, string>
            {
                {"recognizzle", "hej"},
                {"id", id.ToString()}
            };
            return new TransportMessage(headers, new byte[] { 1, 2, 3 });
        }
    }
}