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
using Rebus.Transport.Msmq;

namespace Rebus.Tests.Integration.Msmq
{
    [TestFixture]
    public class TestMsmqExpress : FixtureBase
    {
        BuiltinHandlerActivator _activator;
        IBus _bus;

        protected override void SetUp()
        {
            var queueName = TestConfig.QueueName("expressperf");

            MsmqUtil.Delete(queueName);

            _activator = Using(new BuiltinHandlerActivator());

            _bus = Configure.With(_activator)
                .Logging(l => l.ColoredConsole(LogLevel.Info))
                .Transport(t => t.UseMsmq(queueName))
                .Options(o => o.SetMaxParallelism(100))
                .Start();
        }

        [TestCase(100000, true)]
        [TestCase(100000, false)]
        public async Task TestPerformance(int messageCount, bool express)
        {
            var receivedMessages = 0L;
            _activator.Handle<object>(async msg => Interlocked.Increment(ref receivedMessages));

            while (_bus.Advanced.Workers.Count > 0)
            {
                _bus.Advanced.Workers.RemoveWorker();
            }

            await Task.WhenAll(Enumerable.Range(0, messageCount)
                .Select(i => express ? (object) new ExpressMessage() : new NormalMessage())
                .Select(msg => _bus.SendLocal(msg)));

            var stopwatch = Stopwatch.StartNew();

            while (_bus.Advanced.Workers.Count < 5)
            {
                _bus.Advanced.Workers.AddWorker();
            }

            while (Interlocked.Read(ref receivedMessages) < messageCount)
            {
                Thread.Sleep(1000);
                Console.WriteLine("Got {0} messages...", Interlocked.Read(ref receivedMessages));
            }

            var totalSeconds = stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine("Received {0} messages in {1:0.0} s - that's {2:0.0} msg/s", messageCount, totalSeconds, messageCount/totalSeconds);
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
            public ExpressAttribute() : base(Headers.Express, "")
            {
            }
        }
    }
}