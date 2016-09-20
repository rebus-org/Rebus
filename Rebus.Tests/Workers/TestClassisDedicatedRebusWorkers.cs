using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;

#pragma warning disable 1998

namespace Rebus.Tests.Workers
{
    [TestFixture]
    public class TestClassisDedicatedRebusWorkers : FixtureBase
    {
        BuiltinHandlerActivator _activator;
        InMemNetwork _network;

        protected override void SetUp()
        {
            _activator = Using(new BuiltinHandlerActivator());
            _network = new InMemNetwork();

            Configure.With(_activator)
                .Logging(l => l.Use(new ConsoleLoggerFactory(false)
                {
                    Filters =
                    {
                        //logStatement => logStatement.Level >= LogLevel.Warn
                        //                || logStatement.Type.FullName.Contains("ThreadPoolWorker")
                    }
                }))
                .Transport(t => t.UseInMemoryTransport(_network, "threadpool-workers-test"))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(0);
                    o.SetMaxParallelism(1);
                })
                .Start();
        }

        [TestCase(4)]
        public async Task CanReceiveSomeMessages(int messageCount)
        {
            var counter = new SharedCounter(messageCount);

            _activator.Handle<string>(async message =>
            {
                Console.WriteLine($"Handling message: {message}");
                counter.Decrement();
            });

            await Task.WhenAll(Enumerable.Range(0, messageCount)
                .Select(i => _activator.Bus.SendLocal($"This is message {i}")));

            _activator.Bus.Advanced.Workers.SetNumberOfWorkers(1);

            counter.WaitForResetEvent(100);
        }

        [Test]
        public async Task ContinuationsCanStillUseTheBus()
        {
            const string queueName = "another-queue";

            _network.CreateQueue(queueName);

            _activator.Bus.Advanced.Workers.SetNumberOfWorkers(1);

            var gotMessage = new ManualResetEvent(false);
            var doneHandlingMessage = new ManualResetEvent(false);

            _activator.Handle<string>(async (bus, message) =>
            {
                gotMessage.Set();

                Printt("Got message - waiting 3 s...");

                await Task.Delay(3000);

                Printt("Done waiting :)");

                await bus.Advanced.Routing.Send(queueName, "JAJA DET VIRKER!");

                doneHandlingMessage.Set();
            });

            // send message
            await _activator.Bus.SendLocal("hej med dig");

            // wait for message to be received and then immediately shutdown the bus
            gotMessage.WaitOrDie(TimeSpan.FromSeconds(3), "Did not receive message within timeout");

            CleanUpDisposables();

            // wait for message to have been handled to end
            doneHandlingMessage.WaitOrDie(TimeSpan.FromSeconds(6), "Did not finish handling the message within expected timeframe");

            // wait for message to pop up in the expected queue
            var transportMessage = await _network.WaitForNextMessageFrom(queueName);

            Assert.That(Encoding.UTF8.GetString(transportMessage.Body), Is.EqualTo(@"""JAJA DET VIRKER!"""));
        }

        [Test]
        public async Task PrintThreadNames()
        {
            var threadNames = new ConcurrentQueue<string>();
            var done = new ManualResetEvent(false);

            _activator.Bus.Advanced.Workers.SetNumberOfWorkers(1);

            _activator.Handle<string>(async str =>
            {
                Bim(threadNames);
                await Task.Delay(100);

                Bim(threadNames);
                await Task.Delay(100);

                Bim(threadNames);
                done.Set();
            });

            await _activator.Bus.SendLocal("hej");

            done.WaitOrDie(TimeSpan.FromSeconds(2));

            Console.WriteLine("Thread names:");
            Console.WriteLine(string.Join(Environment.NewLine, threadNames));
        }

        static void Bim(ConcurrentQueue<string> threadNames)
        {
            var threadName = Thread.CurrentThread.Name;
            Printt($"Thread: {threadName}");
            threadNames.Enqueue(threadName);
        }
    }
}