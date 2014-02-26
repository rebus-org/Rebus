using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Tests.Bugs;
using Rebus.Tests.Persistence;
using Rebus.Transports.Sql;
using Rebus.Logging;
using Shouldly;
using Timer = System.Timers.Timer;

namespace Rebus.Tests.Integration
{
    [TestFixture, Category(TestCategories.MsSql)]
    public class TestSimpleSqlSetup : SqlServerFixtureBase
    {
        const string InputQueueName2 = "test.input2";
        const string InputQueueName1 = "test.input1";

        const LogLevel MinLogLevel = LogLevel.Warn;
        const int NumberOfWorkers = 1;

        readonly ConcurrentDictionary<Type, string> endpointMappings = new ConcurrentDictionary<Type, string>();

        BuiltinContainerAdapter adapter1;
        BuiltinContainerAdapter adapter2;

        protected override void DoSetUp()
        {
            DropMessageTable();

            adapter1 = TrackDisposable(new BuiltinContainerAdapter());
            adapter2 = TrackDisposable(new BuiltinContainerAdapter());

            Configure
                .With(adapter1)
                .Logging(l => l.ColoredConsole(MinLogLevel))
                .Transport(t => t.UseSqlServer(ConnectionString, InputQueueName1, "error")
                    .EnsureTableIsCreated()
                    .PurgeInputQueue())
                .MessageOwnership(o => o.Use(this))
                .Behavior(b => b.SetMaxRetriesFor<Exception>(0))
                .CreateBus()
                .Start(NumberOfWorkers);

            Configure
                .With(adapter2)
                .Logging(l => l.ColoredConsole(MinLogLevel))
                .Transport(t => t.UseSqlServer(ConnectionString, InputQueueName2, "error")
                    .EnsureTableIsCreated()
                    .PurgeInputQueue())
                .MessageOwnership(o => o.Use(this))
                .Behavior(b => b.SetMaxRetriesFor<Exception>(0))
                .CreateBus()
                .Start(NumberOfWorkers);
        }

        [TestCase(2)]
        [TestCase(10)]
        [TestCase(40)]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        [TestCase(100000, Ignore = TestCategories.IgnoreLongRunningTests)]
        public void CanSendMessagesFromOneToAnother(int numberOfMessages)
        {
            using (var printStatusTimer = new Timer())
            {
                var sentMessages = 0;
                var receivedMessages = 0;

                printStatusTimer.Interval = 2000;
                printStatusTimer.Elapsed += delegate
                {
                    Console.WriteLine("Sent {0}. Received {1}.", sentMessages, receivedMessages);
                };
                printStatusTimer.Start();

                // bus2 owns System.String
                Map<MyMessage>(InputQueueName2);

                // set up handler that counts the received messages
                var signalWhenAllMessagesHaveBeenReceived = new ManualResetEvent(false);
                var messageTracker = new ConcurrentDictionary<int, int>();

                adapter2.Handle<MyMessage>(msg =>
                {
                    Interlocked.Increment(ref receivedMessages);

                    messageTracker.AddOrUpdate(msg.Id, newId => 1, (existingId, count) => count + 1);

                    if (receivedMessages >= numberOfMessages)
                    {
                        signalWhenAllMessagesHaveBeenReceived.Set();
                    }
                });

                // use bus1 to send appropriate number of messages
                Enumerable.Range(0, numberOfMessages)
                    .Select(i =>
                    {
                        var id = i + 1;
                        return
                            new MyMessage
                            {
                                Id = id,
                                Label = string.Format("Message # {0}", id)
                            };
                    })
                    .ToList()
                    .ForEach(message =>
                    {
                        adapter1.Bus.Send(message);
                        if (!messageTracker.TryAdd(message.Id, 0))
                        {
                            throw new OmgWtfException(string.Format("OMG it seems that the ID {0} was already present in the tracker!!", message.Id));
                        }
                        Interlocked.Increment(ref sentMessages);
                    });

                var timeout = TimeSpan.FromMilliseconds(numberOfMessages * 2) + TimeSpan.FromSeconds(5);

                if (!signalWhenAllMessagesHaveBeenReceived.WaitOne(timeout))
                {
                    Assert.Fail("Did not receive expected {0} messages within {1} timeout",
                        numberOfMessages, timeout);
                }

                Console.WriteLine("Waiting 2 seconds...");
                // give additional messages a chance to arrive (which shouldn't happen, but let's just see if it does...)
                Thread.Sleep(2.Seconds());

                messageTracker.Count.ShouldBe(numberOfMessages);

                if (messageTracker.Values.Any(v => v != 1))
                {
                    Assert.Fail(@"Not all messages were received exactly once!

{0}", string.Join(", ", messageTracker.Where(kvp => kvp.Value != 1)
                        .Select(kvp => string.Format("{0}: {1}", kvp.Key, kvp.Value))));
                }
            }
        }

        void DropMessageTable()
        {
            if (GetTableNames()
                .Contains(SqlServerMessageQueueConfigurationExtension.DefaultMessagesTableName))
            {
                ExecuteCommand(string.Format("drop table [{0}]",
                                             SqlServerMessageQueueConfigurationExtension.DefaultMessagesTableName));
            }
        }

        public override string GetEndpointFor(Type messageType)
        {
            string endpoint;
            if (endpointMappings.TryGetValue(messageType, out endpoint))
                return endpoint;

            throw new ArgumentException(string.Format("Don't have an endpoint mapping for {0}", messageType));
        }

        void Map<TMessage>(string endpoint)
        {
            endpointMappings[typeof(TMessage)] = endpoint;
        }

        class MyMessage
        {
            public string Label { get; set; }
            public int Id { get; set; }
        }
    }
}