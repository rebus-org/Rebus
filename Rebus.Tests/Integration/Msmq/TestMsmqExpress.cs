using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Transport.Msmq;
#pragma warning disable 1998

namespace Rebus.Tests.Integration.Msmq
{
    [TestFixture, Category(Categories.Msmq)]
    public class TestMsmqExpress : FixtureBase
    {
        BuiltinHandlerActivator _activator;
        IBus _bus;

        protected override void SetUp()
        {
            var queueName = TestConfig.GetName("expressperf");

            MsmqUtil.Delete(queueName);

            _activator = Using(new BuiltinHandlerActivator());

            _bus = Configure.With(_activator)
                .Logging(l => l.ColoredConsole(LogLevel.Info))
                .Transport(t => t.UseMsmq(queueName))
                .Options(o => o.SetMaxParallelism(100))
                .Start();
        }

        [TestCase(10000, true, Ignore = true)]
        [TestCase(10000, false, Ignore = true)]
        [TestCase(100, true)]
        [TestCase(100, false)]
        public async Task TestPerformance(int messageCount, bool express)
        {
            var receivedMessages = 0L;
            _activator.Handle<object>(async msg => Interlocked.Increment(ref receivedMessages));

            _bus.Advanced.Workers.SetNumberOfWorkers(0);

            await Task.WhenAll(Enumerable.Range(0, messageCount)
                .Select(i => express ? (object)new ExpressMessage() : new NormalMessage())
                .Select(msg => _bus.SendLocal(msg)));

            var stopwatch = Stopwatch.StartNew();

            _bus.Advanced.Workers.SetNumberOfWorkers(5);

            while (Interlocked.Read(ref receivedMessages) < messageCount)
            {
                Thread.Sleep(1000);
                Console.WriteLine("Got {0} messages...", Interlocked.Read(ref receivedMessages));
            }

            var totalSeconds = stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine("Received {0} messages in {1:0.0} s - that's {2:0.0} msg/s", messageCount, totalSeconds, messageCount / totalSeconds);
        }

        [Express]
        class ExpressMessage
        {
        }

        class NormalMessage
        {
        }

        class ExpressAttribute : HeaderAttribute
        {
            public ExpressAttribute()
                : base(Headers.Express, "")
            {
            }
        }
    }
}