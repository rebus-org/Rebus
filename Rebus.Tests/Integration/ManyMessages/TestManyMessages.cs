using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Tests.Extensions;
using Timer = System.Timers.Timer;

namespace Rebus.Tests.Integration.ManyMessages
{
    [TestFixture]
    public class MsmqTestManyMessages : TestManyMessages<MsmqBusFactory> { }

    [TestFixture]
    public class InMemTestManyMessages : TestManyMessages<InMemoryBusFactory> { }

    [TestFixture]
    public class SqlServerTestManyMessages : TestManyMessages<SqlServerBusFactory> { }

    public abstract class TestManyMessages<TBusFactory> : FixtureBase where TBusFactory : IBusFactory, new()
    {
        TBusFactory _busFactory;

        protected override void SetUp()
        {
            _busFactory = new TBusFactory();

            RebusLoggerFactory.Current = new ConsoleLoggerFactory(false)
            {
                MinLevel = LogLevel.Info
            };
        }

        protected override void TearDown()
        {
            _busFactory.Cleanup();
        }

        [TestCase(10)]
        public async Task SendAndReceiveManyMessages(int messageCount)
        {
            var allMessagesReceived = new ManualResetEvent(false);
            var idCounts = new ConcurrentDictionary<int, int>();
            var sentMessages = 0;
            var receivedMessages = 0;
            var stopWatch = new Stopwatch();
            var bus1 = _busFactory.GetBus<MessageWithId>(TestConfig.QueueName("input1"),
                async msg =>
                {
                    idCounts.AddOrUpdate(msg.Id, 1, (id, old) => old + 1);

                    Interlocked.Increment(ref receivedMessages);

                    if (receivedMessages >= messageCount)
                    {
                        stopWatch.Stop();
                        Console.WriteLine("DONE: took time:" + stopWatch.ElapsedMilliseconds+"ms");
                        allMessagesReceived.Set();

                    }
                });

            var messagesToSend = Enumerable.Range(0, messageCount)
                .Select(id => new MessageWithId(id))
                .ToList();

            using (var printTimer = new Timer(5000))
            {
                printTimer.Elapsed += delegate { Console.WriteLine("Sent: {0}, Received: {1}", sentMessages, receivedMessages); };
                printTimer.Start();
                stopWatch.Start();
                Console.WriteLine("Sending {0} messages", messageCount);
                await Task.WhenAll(messagesToSend.Select(async msg =>
                {
                    await bus1.SendLocal(msg);
                    Interlocked.Increment(ref sentMessages);
                }));

                var timeout = TimeSpan.FromSeconds(messageCount * 0.01 + 100);
                Console.WriteLine("Waiting up to {0} seconds", timeout.TotalSeconds);
                allMessagesReceived.WaitOrDie(timeout, errorMessageFactory: () => GenerateErrorText(idCounts));
            }

            Console.WriteLine("Waiting one more second in case messages are still dripping in...");
            await Task.Delay(1000);

            var errorText = GenerateErrorText(idCounts);

            Assert.That(idCounts.Count, Is.EqualTo(messageCount), errorText);
            Assert.That(idCounts.All(c => c.Value == 1), errorText);
        }

        static string GenerateErrorText(ConcurrentDictionary<int, int> idCounts)
        {
            var errorText = string.Format("The following IDs were received != 1 times: {0}",
                string.Join(", ",
                    idCounts.Where(kvp => kvp.Value != 1)
                        .OrderBy(kvp => kvp.Value)
                        .Select(kvp => string.Format("{0} (x {1})", kvp.Key, kvp.Value))));
            return errorText;
        }

        class MessageWithId
        {
            public MessageWithId(int id)
            {
                Id = id;
            }

            public int Id { get; private set; }

            public override string ToString()
            {
                return string.Format("<msg {0}>", Id);
            }
        }
    }

    public interface IBusFactory
    {
        IBus GetBus<TMessage>(string inputQueueAddress, Func<TMessage, Task> handler);
        void Cleanup();
    }
}