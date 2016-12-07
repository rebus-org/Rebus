using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Backoff;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport;
using Rebus.Transport.InMem;
using Rebus.Workers.ThreadPoolBased;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Backoff
{
    public class TestBackoffBehaviorWhenBusy : FixtureBase
    {
        BuiltinHandlerActivator _activator;
        BackoffSnitch _snitch;

        public TestBackoffBehaviorWhenBusy()
        {
            _activator = Using(new BuiltinHandlerActivator());

            _snitch = new BackoffSnitch();

            Configure.With(_activator)
                .Logging(l => l.Console(LogLevel.Info))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "busy-test"))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(1);
                    o.SetMaxParallelism(500);

                    o.SetBackoffTimes(TimeSpan.FromSeconds(0.2));

                    // install the snitch
                    o.Decorate<ISyncBackoffStrategy>(c =>
                    {
                        var syncBackoffStrategy = c.Get<ISyncBackoffStrategy>();
                        _snitch.SyncBackoffStrategy = syncBackoffStrategy;
                        return _snitch;
                    });

                    o.Decorate<ITransport>(c =>
                    {
                        var transport = c.Get<ITransport>();

                        return new IntroducerOfLatency(transport, receiveLatencyMs: 10);
                    });
                })
                .Start();
        }

        [Theory]
        [InlineData(100000)]
        public async Task DoesNotBackOffAtAllWhenBusy(int messageCount)
        {
            var counter = new SharedCounter(messageCount);

            _activator.Handle<string>(async str =>
            {
                await Task.Delay(1);
                counter.Decrement();
            });

            var startTime = DateTime.UtcNow;

            await Task.Delay(TimeSpan.FromSeconds(5));

            Printt("Sending 100k msgs");

            await Task.WhenAll(Enumerable.Range(0, messageCount)
                .Select(i => _activator.Bus.SendLocal($"THIS IS MESSAGE {i}")));

            Printt("Receiving them...");

            _activator.Bus.Advanced.Workers.SetNumberOfWorkers(1);

            counter.WaitForResetEvent(120);

            Printt("Done... waiting a little extra");
            await Task.Delay(TimeSpan.FromSeconds(5));

            var stopTime = DateTime.UtcNow;

            var waitsPerSecond = _snitch.WaitTimes
                .GroupBy(t => t.RoundTo(TimeSpan.FromSeconds(1)))
                .ToDictionary(g => g.Key, g => g.Count());

            var waitNoMessagesPerSecond = _snitch.WaitNoMessageTimes
                .GroupBy(t => t.RoundTo(TimeSpan.FromSeconds(1)))
                .ToDictionary(g => g.Key, g => g.Count());

            var seconds = startTime.RoundTo(TimeSpan.FromSeconds(1)).To(stopTime.RoundTo(TimeSpan.FromSeconds(1)))
                .GetIntervals(TimeSpan.FromSeconds(1));

            Console.WriteLine(string.Join(Environment.NewLine,
                seconds.Select(time => $"{time}: {new string('.', waitsPerSecond.GetValueOrDefault(time))}{new string('*', waitNoMessagesPerSecond.GetValueOrDefault(time))}")));
        }

        class BackoffSnitch : ISyncBackoffStrategy
        {
            readonly ConcurrentQueue<DateTime> _waitTimes = new ConcurrentQueue<DateTime>();
            readonly ConcurrentQueue<DateTime> _waitNoMessageTimes = new ConcurrentQueue<DateTime>();

            public ISyncBackoffStrategy SyncBackoffStrategy { get; set; }

            public IEnumerable<DateTime> WaitTimes => _waitTimes;
            public IEnumerable<DateTime> WaitNoMessageTimes => _waitNoMessageTimes;

            public void Reset()
            {
                SyncBackoffStrategy.Reset();
            }

            public void WaitNoMessage()
            {
                _waitNoMessageTimes.Enqueue(DateTime.UtcNow);
                SyncBackoffStrategy.WaitNoMessage();
            }

            public void Wait()
            {
                _waitTimes.Enqueue(DateTime.UtcNow);
                SyncBackoffStrategy.Wait();
            }

            public void WaitError()
            {
                SyncBackoffStrategy.WaitError();
            }
        }
    }
}