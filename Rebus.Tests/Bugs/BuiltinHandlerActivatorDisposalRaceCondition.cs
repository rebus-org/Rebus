using System.Collections.Concurrent;
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
#pragma warning disable 1998

namespace Rebus.Tests.Bugs;

[TestFixture]
[Description("Verifies that the last message handled by the endpoint does not get its handler called with a NULL bus because of a race condition in BuiltinHandlerActivator")]
public class BuiltinHandlerActivatorDisposalRaceCondition : FixtureBase
{
    [Test]
    public void DoesNotDispatchMessageWithNullBus()
    {
        var busInstances = new ConcurrentQueue<IBus>();

        using (var activator = new BuiltinHandlerActivator())
        {
            activator.Handle<string>(async (bus, message) =>
            {
                busInstances.Enqueue(bus);
            });

            Configure.With(activator)
                .Logging(l => l.ColoredConsole(LogLevel.Warn))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "race-condition"))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(1);
                    o.SetMaxParallelism(1);
                })
                .Start();

            Task.WaitAll(Enumerable.Range(0, 1000)
                .Select(i => activator.Bus.SendLocal($"message-{i}"))
                .ToArray());
        }

        Thread.Sleep(1000);

        var numberOfNulls = busInstances.Count(i => i == null);

        Assert.That(numberOfNulls, Is.EqualTo(0),
            "Did not expect any messages to be dispatched with a NULL bus instance");
    }
}