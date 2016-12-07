using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Backoff;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;
using Xunit;

namespace Rebus.Tests.Backoff
{
    public class BackoffBehaviorIntegrationTest : FixtureBase
    {
        [Fact]
        public async Task CheckIdleBehavior()
        {
            var activator = Using(new BuiltinHandlerActivator());
            var transportDecorator = new LoggingTransportDecorator();

            Configure.With(activator)
                .Transport(t =>
                {
                    t.UseInMemoryTransport(new InMemNetwork(), "backoff-check");
                    t.Decorate(c =>
                    {
                        var transport = c.Get<ITransport>();
                        transportDecorator.Transport = transport;
                        return transportDecorator;
                    });
                })
                .Options(o =>
                {
                    o.SetBackoffTimes(
                        // first five  seconds
                        TimeSpan.FromSeconds(0.01),
                        TimeSpan.FromSeconds(0.01),
                        TimeSpan.FromSeconds(0.01),
                        TimeSpan.FromSeconds(0.01),
                        TimeSpan.FromSeconds(0.01),

                        // next five seconds
                        TimeSpan.FromSeconds(0.1),
                        TimeSpan.FromSeconds(0.1),
                        TimeSpan.FromSeconds(0.1),
                        TimeSpan.FromSeconds(0.1),
                        TimeSpan.FromSeconds(0.1),

                        // the rest of the time
                        TimeSpan.FromSeconds(0.5)
                        );
                })
                .Start();

            await Task.Delay(TimeSpan.FromSeconds(15));

            var recordedReceiveTimes = transportDecorator.ReceiveTimes.ToList();

            var results = recordedReceiveTimes
                .GroupBy(t => t.RoundTo(TimeSpan.FromSeconds(1)))
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Time = g.Key,
                    Count = g.Count()
                })
                .ToList();

            Console.WriteLine(string.Join(Environment.NewLine, results.Select(r => $"{r.Time}: {new string('*', r.Count)}")));

            var firstPeriodMedian = results.Skip(0).Take(5).GetMedianBy(g => g.Time);

            var secondPeriodMedian = results.Skip(5).Take(5).GetMedianBy(g => g.Time);

            var thirdPeriodMedian = results.Skip(10).Take(5).GetMedianBy(g => g.Time);

            Console.WriteLine($" First period median: {firstPeriodMedian}");
            Console.WriteLine($"Second period median: {secondPeriodMedian}");
            Console.WriteLine($" Third period median: {thirdPeriodMedian}");

            Assert.True(firstPeriodMedian.Count > (4 * secondPeriodMedian.Count),
                "Expected receive calls during the first period to be more than four times as frequent as during the second period");

            Assert.True(secondPeriodMedian.Count > (4 * thirdPeriodMedian.Count),
                "Expected receive calls during the second period to be more than four times as frequent as during the third period");
        }

        class LoggingTransportDecorator : ITransport
        {
            readonly ConcurrentQueue<DateTime> _receiveTimes = new ConcurrentQueue<DateTime>();

            public IEnumerable<DateTime> ReceiveTimes => _receiveTimes;

            public ITransport Transport { get; set; }

            public void CreateQueue(string address)
            {
                Transport.CreateQueue(address);
            }

            public Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
            {
                return Transport.Send(destinationAddress, message, context);
            }

            public Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
            {
                _receiveTimes.Enqueue(DateTime.UtcNow);
                return Transport.Receive(context, cancellationToken);
            }

            public string Address => Transport.Address;
        }
    }
}