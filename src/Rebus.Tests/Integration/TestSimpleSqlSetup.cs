using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Tests.Persistence;
using Rebus.Transports.Sql;
using Rebus.Logging;
using Shouldly;

namespace Rebus.Tests.Integration
{
    [TestFixture, Category(TestCategories.MsSql)]
    public class TestSimpleSqlSetup : SqlServerFixtureBase, IDetermineMessageOwnership
    {
        const string InputQueueName2 = "test.input2";
        const string InputQueueName1 = "test.input1";

        const LogLevel MinLogLevel = LogLevel.Warn;
        const int NumberOfWorkers = 15;

        readonly ConcurrentDictionary<Type, string> endpointMappings = new ConcurrentDictionary<Type, string>();

        BuiltinContainerAdapter adapter1;
        BuiltinContainerAdapter adapter2;
        IBus bus1;
        IBus bus2;

        protected override void DoSetUp()
        {
            if (GetTableNames().Contains(SqlServerMessageQueueConfigurationExtension.DefaultMessagesTableName))
            {
                ExecuteCommand(string.Format("drop table [{0}]",
                                             SqlServerMessageQueueConfigurationExtension.DefaultMessagesTableName));
            }

            adapter1 = new BuiltinContainerAdapter();
            adapter2 = new BuiltinContainerAdapter();

            var rebus1 =
                (RebusBus)Configure
                               .With(adapter1)
                               .Logging(l => l.ColoredConsole(MinLogLevel))
                               .Transport(t => t.UseSqlServer(ConnectionString, InputQueueName1, "error")
                                                .EnsureTableIsCreated()
                                                .PurgeInputQueue())
                               .MessageOwnership(o => o.Use(this))
                               .CreateBus();

            var rebus2 =
                (RebusBus)Configure
                               .With(adapter2)
                               .Logging(l => l.ColoredConsole(MinLogLevel))
                               .Transport(t => t.UseSqlServer(ConnectionString, InputQueueName2, "error")
                                                .EnsureTableIsCreated()
                                                .PurgeInputQueue())
                               .MessageOwnership(o => o.Use(this))
                               .CreateBus();

            rebus1.Start(NumberOfWorkers);
            rebus2.Start(NumberOfWorkers);

            bus1 = adapter1.Bus;
            bus2 = adapter2.Bus;
        }

        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        [TestCase(100000)]
        public void CanSendMessagesFromOneToAnother(int numberOfMessages)
        {
            // bus2 owns System.String
            Map<MyMessage>(InputQueueName2);

            // set up handler that counts the received messages
            var signalWhenAllMessagesHaveBeenReceived = new ManualResetEvent(false);
            var receivedMessages = 0;
            var locker = new object();
            var messageTracker = new Dictionary<int, bool>();

            adapter2.Handle<MyMessage>(msg =>
                {
                    var result = Interlocked.Increment(ref receivedMessages);

                    if (result < 200)
                    {
                        Console.WriteLine("Got message : {0}", msg.Label);
                    }
                    else if (result % 200 == 0)
                    {
                        Console.WriteLine("Got {0} messages now...", result);
                    }

                    lock (locker)
                    {
                        if (!messageTracker.ContainsKey(msg.Id))
                        {
                            throw new ArgumentException(string.Format("Oh noes! Could not find ID {0} in dictionary", msg.Id));
                        }

                        if (messageTracker[msg.Id])
                        {
                            throw new ArgumentException(string.Format("Oh noes! ID {0} already received!", msg.Id));
                        }

                        messageTracker[msg.Id] = true;
                    }

                    if (result == numberOfMessages)
                    {
                        signalWhenAllMessagesHaveBeenReceived.Set();
                    }
                });

            // use bus1 to send appropriate number of messages
            Enumerable.Range(0, numberOfMessages)
                      .Select(i => new MyMessage
                                       {
                                           Id = i + 1,
                                           Label = string.Format("Message # {0}", i)
                                       })
                      .ToList()
                      .ForEach(message =>
                          {
                              lock (locker)
                              {
                                  bus1.Send(message);
                                  messageTracker.Add(message.Id, false);
                              }
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

            if (messageTracker.Values.Any(v => !v))
            {
                Assert.Fail("Did not receive the following messages: {0}",
                            string.Join(", ", messageTracker.Where(kvp => !kvp.Value)
                                                            .Select(kvp => kvp.Key)));
            }
        }

        protected override void DoTearDown()
        {
            adapter1.Dispose();
            adapter2.Dispose();
        }

        public string GetEndpointFor(Type messageType)
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