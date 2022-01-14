using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
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

#pragma warning disable 1998

namespace Rebus.Tests.Backoff;

[TestFixture]
public class TestBackoffBehaviorWhenBusy : FixtureBase
{
    BuiltinHandlerActivator _activator;
    BackoffSnitch _snitch;

    IBusStarter _starter;

    protected override void SetUp()
    {
        _activator = Using(new BuiltinHandlerActivator());

        _snitch = new BackoffSnitch();

        _starter = Configure.With(_activator)
            .Logging(l => l.Console(LogLevel.Info))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "busy-test"))
            .Options(o =>
            {
                o.SetNumberOfWorkers(1);
                o.SetMaxParallelism(500);

                o.SetBackoffTimes(TimeSpan.FromSeconds(0.2));

                // install the snitch
                o.Decorate<IBackoffStrategy>(c =>
                {
                    var backoffStrategy = c.Get<IBackoffStrategy>();
                    _snitch.BackoffStrategy = backoffStrategy;
                    return _snitch;
                });

                o.Decorate<ITransport>(c =>
                {
                    var transport = c.Get<ITransport>();

                    return new IntroducerOfLatency(transport, receiveLatencyMs: 10);
                });
            })
            .Create();
    }

    [TestCase(100000)]
    public async Task DoesNotBackOffAtAllWhenBusy(int messageCount)
    {
        var counter = new SharedCounter(messageCount);

        _activator.Handle<string>(async str =>
        {
            await Task.Delay(1);
            counter.Decrement();
        });

        _starter.Start();

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
            seconds.Select(time => $"{time}: {new string('.', waitsPerSecond.GetOrDefault(time))}{new string('*', waitNoMessagesPerSecond.GetOrDefault(time))}")));
    }

    class BackoffSnitch : IBackoffStrategy
    {
        readonly ConcurrentQueue<DateTime> _waitTimes = new ConcurrentQueue<DateTime>();
        readonly ConcurrentQueue<DateTime> _waitNoMessageTimes = new ConcurrentQueue<DateTime>();

        public IBackoffStrategy BackoffStrategy { get; set; }

        public IEnumerable<DateTime> WaitTimes => _waitTimes;
        public IEnumerable<DateTime> WaitNoMessageTimes => _waitNoMessageTimes;

        public void Reset()
        {
            BackoffStrategy.Reset();
        }

        public void WaitNoMessage(CancellationToken token)
        {
            _waitNoMessageTimes.Enqueue(DateTime.UtcNow);
            BackoffStrategy.WaitNoMessage(token);
        }

        public Task WaitNoMessageAsync(CancellationToken token)
        {
            _waitNoMessageTimes.Enqueue(DateTime.UtcNow);
            return BackoffStrategy.WaitNoMessageAsync(token);
        }

        public void Wait(CancellationToken token)
        {
            _waitTimes.Enqueue(DateTime.UtcNow);
            BackoffStrategy.Wait(token);
        }

        public Task WaitAsync(CancellationToken token)
        {
            _waitTimes.Enqueue(DateTime.UtcNow);
            return BackoffStrategy.WaitAsync(token);
        }

        public void WaitError(CancellationToken token)
        {
            BackoffStrategy.WaitError(token);
        }

        public Task WaitErrorAsync(CancellationToken token)
        {
            return BackoffStrategy.WaitErrorAsync(token);
        }
    }
}