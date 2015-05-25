using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Tests;
using Rebus.Tests.Extensions;
using Rebus.Transport;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture]
    public class AzureServiceBusPrefetchTest : FixtureBase
    {
        const string QueueName = "prefetch";

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
        [TestCase(10, 10000)]
        [TestCase(20, 10000)]
        [TestCase(30, 10000)]
        [TestCase(50, 10000)]
        [TestCase(100, 10000)]
        [TestCase(200, 10000)]
        public void WorksWithPrefetch(int prefetch, int numberOfMessages)
        {
            AdjustLogging(LogLevel.Info);

            var activator = new BuiltinHandlerActivator();
            var receivedMessages = 0;
            var done = new ManualResetEvent(false);

            activator.Handle<string>(async str =>
            {
                Interlocked.Increment(ref receivedMessages);

                if (receivedMessages == numberOfMessages)
                {
                    done.Set();
                }
            });

            Console.WriteLine("Sending {0} messages", numberOfMessages);

            using (var transport = GetTransport())
            {
                var tasks = Enumerable.Range(0, numberOfMessages)
                    .Select(i => string.Format("THIS IS MESSAGE # {0}", i))
                    .Select(async msg =>
                    {
                        using (var context = new DefaultTransactionContext())
                        {
                            var headers = DefaultHeaders();
                            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg));
                            var transportMessage = new TransportMessage(headers, body);

                            await transport.Send(QueueName, transportMessage, context);

                            await context.Complete();
                        }
                    })
                    .ToArray();

                Task.WhenAll(tasks).Wait();
            }

            Console.WriteLine("Receiving {0} messages", numberOfMessages);

            var stopwatch = Stopwatch.StartNew();

            using (Configure.With(activator)
                .Transport(t =>
                {
                    t.UseAzureServiceBus(AzureServiceBusTransportFactory.ConnectionString, QueueName)
                        .EnablePrefetching(prefetch);
                })
                .Options(o =>
                {
                    o.SetNumberOfWorkers(5);
                    o.SetMaxParallelism(10);
                })
                .Start())
            {
                done.WaitOrDie(TimeSpan.FromSeconds(numberOfMessages * 0.1 + 3));
            }

            var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

            Console.WriteLine("Receiving {0} messages took {1:0.0} s - that's {2:0.0} msg/s",
                numberOfMessages, elapsedSeconds, numberOfMessages / elapsedSeconds);
        }

        Dictionary<string, string> DefaultHeaders()
        {
            return new Dictionary<string, string>
            {
                {Headers.MessageId, Guid.NewGuid().ToString()},
                {Headers.ContentType, "application/json;charset=utf-8"},
            };
        }

        static AzureServiceBusTransport GetTransport()
        {
            var transport = new AzureServiceBusTransport(AzureServiceBusTransportFactory.ConnectionString, QueueName);
            transport.Initialize();
            transport.PurgeInputQueue();
            return transport;
        }
    }
}