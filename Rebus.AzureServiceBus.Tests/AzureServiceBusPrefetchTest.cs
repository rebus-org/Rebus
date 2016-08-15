using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.Config;
using Rebus.AzureServiceBus.Tests.Factories;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Tests;
using Rebus.Threading.TaskParallelLibrary;
using Rebus.Transport;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture(AzureServiceBusMode.Basic), Category(TestCategory.Azure)]
    [TestFixture(AzureServiceBusMode.Standard), Category(TestCategory.Azure)]
    public class AzureServiceBusPrefetchTest : FixtureBase
    {
        readonly AzureServiceBusMode _mode;
        readonly string _queueName = TestConfig.QueueName("prefetch");

        public AzureServiceBusPrefetchTest(AzureServiceBusMode mode)
        {
            _mode = mode;
        }

        /// <summary>
        /// Initial: 
        ///     Receiving 1000 messages took 98,5 s - that's 10,2 msg/s
        /// 
        /// Removing auto-peek lock renewal:
        ///     Receiving 1000 messages took 4,8 s - that's 210,0 msg/s
        ///     Receiving 10000 messages took 71,9 s - that's 139,1 msg/s
        ///     Receiving 10000 messages took 85,1 s - that's 117,6 msg/s
        ///     Receiving 10000 messages took 127,5 s - that's 78,4 msg/s
        ///     Receiving 10000 messages took 98,1 s - that's 101,9 msg/s
        /// 
        /// With prefetch 10:
        ///     Receiving 10000 messages took 35,7 s - that's 280,3 msg/s
        ///     Receiving 10000 messages took 55,1 s - that's 181,5 msg/s
        /// 
        /// With prefetch 100:
        ///     Receiving 10000 messages took 31,3 s - that's 319,4 msg/s
        /// 
        /// With prefetch 20:
        ///     Receiving 10000 messages took 30,3 s - that's 330,1 msg/s
        /// 
        /// With prefetch 10:
        ///     Receiving 10000 messages took 28,8 s - that's 347,6 msg/s
        /// 
        /// </summary>
        [TestCase(10, 1000)]
        [TestCase(50, 1000)]
        [TestCase(100, 1000)]
        [TestCase(10, 10000, Ignore = true)]
        [TestCase(20, 10000, Ignore = true)]
        [TestCase(30, 10000, Ignore = true)]
        [TestCase(50, 10000, Ignore = true)]
        [TestCase(100, 10000, Ignore = true)]
        public void WorksWithPrefetch(int prefetch, int numberOfMessages)
        {
            var activator = Using(new BuiltinHandlerActivator());
            var counter = new SharedCounter(numberOfMessages);

            Using(counter);

            activator.Handle<string>(async str =>
            {
                counter.Decrement();
            });

            Console.WriteLine("Sending {0} messages", numberOfMessages);

            var transport = GetTransport();
            var tasks = Enumerable.Range(0, numberOfMessages)
                .Select(i => $"THIS IS MESSAGE # {i}")
                .Select(async msg =>
                {
                    using (var context = new DefaultTransactionContext())
                    {
                        var headers = DefaultHeaders();
                        var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg));
                        var transportMessage = new TransportMessage(headers, body);

                        await transport.Send(_queueName, transportMessage, context);

                        await context.Complete();
                    }
                })
                .ToArray();

            Task.WhenAll(tasks).Wait();

            Console.WriteLine("Receiving {0} messages", numberOfMessages);

            var stopwatch = Stopwatch.StartNew();

            Configure.With(activator)
                .Transport(t =>
                {
                    t.UseAzureServiceBus(StandardAzureServiceBusTransportFactory.ConnectionString, _queueName, _mode)
                        .EnablePrefetching(prefetch);
                })
                .Options(o =>
                {
                    o.SetNumberOfWorkers(5);
                    o.SetMaxParallelism(10);
                })
                .Start();

            counter.WaitForResetEvent(timeoutSeconds: (int)(numberOfMessages * 0.1 + 3));

            var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

            Console.WriteLine("Receiving {0} messages took {1:0.0} s - that's {2:0.0} msg/s",
                numberOfMessages, elapsedSeconds, numberOfMessages / elapsedSeconds);
        }

        protected override void TearDown()
        {
            //StandardAzureServiceBusTransportFactory.DeleteQueue(_queueName);
        }

        Dictionary<string, string> DefaultHeaders()
        {
            return new Dictionary<string, string>
            {
                {Headers.MessageId, Guid.NewGuid().ToString()},
                {Headers.ContentType, "application/json;charset=utf-8"},
            };
        }

        ITransport GetTransport()
        {
            var consoleLoggerFactory = new ConsoleLoggerFactory(false);
            var asyncTaskFactory = new TplAsyncTaskFactory(consoleLoggerFactory);
            var connectionString = StandardAzureServiceBusTransportFactory.ConnectionString;
            var busLifetimeEvents = new BusLifetimeEvents();

            if (_mode == AzureServiceBusMode.Basic)
            {
                var basicTransport = new BasicAzureServiceBusTransport(connectionString, _queueName, consoleLoggerFactory, asyncTaskFactory);
                Using(basicTransport);
                basicTransport.Initialize();
                basicTransport.PurgeInputQueue();
                return basicTransport;
            }
            var transport = new AzureServiceBusTransport(connectionString, _queueName, consoleLoggerFactory, asyncTaskFactory);
            Using(transport);
            transport.Initialize();
            transport.PurgeInputQueue();
            return transport;
        }
    }
}