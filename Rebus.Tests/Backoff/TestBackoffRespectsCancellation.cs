using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Backoff;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
using Rebus.Workers.ThreadPoolBased;

namespace Rebus.Tests.Backoff;

[TestFixture]
public class TestBackoffRespectsCancellation : FixtureBase
{
    private IBus _bus;
    private BackoffSnitch _snitch;

    protected override void SetUp()
    {
        var activator = Using(new BuiltinHandlerActivator());

        _snitch = new BackoffSnitch();

        _bus = Configure.With(activator)
            .Logging(l => l.Console(LogLevel.Info))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "cancellation-test"))
            .Options(o =>
            {
                o.SetMaxParallelism(1);
                o.SetNumberOfWorkers(1);
                o.SetBackoffTimes(TimeSpan.FromDays(1));
                o.SetWorkerShutdownTimeout(TimeSpan.FromMinutes(1));

                // install the snitch
                o.Decorate<IBackoffStrategy>(c =>
                {
                    var backoffStrategy = c.Get<IBackoffStrategy>();
                    _snitch.BackoffStrategy = backoffStrategy;
                    return _snitch;
                });
            })
            .Start();
    }

    [TestCase]
    public void BackoffRespectsCancellation()
    {
        // Wait until the backoff strategy has entered an wait cycle 
        _snitch.WaitNoMessageEntered.WaitOrDie(TimeSpan.FromSeconds(10),
            "Backoff strategy did not enter an wait cycle within the expected timeframe!");

        var shutdownSignal = new ManualResetEvent(false);
        var thread = new Thread(() =>
        {
            _bus.Dispose();
            shutdownSignal.Set();
        });

        thread.Start();
        shutdownSignal.WaitOrDie(TimeSpan.FromSeconds(1),
            "Rebus did not shut down within the expected timeframe because the worker " +
            "did not return from its backoff wait cycle!");
    }

    private class BackoffSnitch : IBackoffStrategy
    {
        public IBackoffStrategy BackoffStrategy { get; set; }

        public ManualResetEvent WaitNoMessageEntered { get; }
            = new ManualResetEvent(false);

        public void Reset()
        {
            BackoffStrategy.Reset();
        }

        public void WaitNoMessage(CancellationToken token)
        {
            BackoffStrategy.WaitNoMessage(token);
        }

        public async Task WaitNoMessageAsync(CancellationToken token)
        {
            WaitNoMessageEntered.Set();
            await BackoffStrategy.WaitNoMessageAsync(token);
        }

        public void Wait(CancellationToken token)
        {
            BackoffStrategy.Wait(token);
        }

        public Task WaitAsync(CancellationToken token)
        {
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