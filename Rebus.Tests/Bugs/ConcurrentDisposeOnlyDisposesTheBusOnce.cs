using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Bugs;

[TestFixture]
[Description("Verifies that calling Dispose on the bus from several threads at the same time only disposes the bus once")]
public class ConcurrentDisposeOnlyDisposesTheBusOnce : FixtureBase
{
    [Test]
    [Repeat(200)]
    public void BusIsOnlyDisposedOnce()
    {
        const int numberOfThreads = 4;

        var busDisposingCount = 0;
        var busDisposedCount = 0;

        using var activator = new BuiltinHandlerActivator();

        var bus = Configure.With(activator)
            .Logging(l => l.Console(LogLevel.Warn))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "concurrent-dispose"))
            .Options(o =>
            {
                o.SetNumberOfWorkers(1);
                o.Decorate(c =>
                {
                    var events = c.Get<BusLifetimeEvents>();
                    events.BusDisposing += () => Interlocked.Increment(ref busDisposingCount);
                    events.BusDisposed += () => Interlocked.Increment(ref busDisposedCount);
                    return events;
                });
            })
            .Start();

        using var barrier = new Barrier(numberOfThreads);

        var tasks = Enumerable.Range(0, numberOfThreads)
            .Select(_ => Task.Factory.StartNew(() =>
            {
                barrier.SignalAndWait();
                bus.Dispose();
            }, TaskCreationOptions.LongRunning))
            .ToArray();

        Task.WaitAll(tasks);

        Assert.That(busDisposingCount, Is.EqualTo(1), "Expected the BusDisposing event to be raised exactly once");
        Assert.That(busDisposedCount, Is.EqualTo(1), "Expected the BusDisposed event to be raised exactly once");
    }
}
