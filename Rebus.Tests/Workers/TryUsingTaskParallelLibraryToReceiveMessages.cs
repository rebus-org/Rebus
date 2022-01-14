using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
using Rebus.Workers.TplBased;
// ReSharper disable RedundantArgumentDefaultValue
#pragma warning disable 1998

namespace Rebus.Tests.Workers;

[TestFixture]
public class TryUsingTaskParallelLibraryToReceiveMessages : FixtureBase
{
    public enum WorkerFactory { DedicatedWorkerThreads, TaskParallelLibrary }

    [TestCase(3, 1, 1, WorkerFactory.DedicatedWorkerThreads)]
    [TestCase(3, 1, 1, WorkerFactory.TaskParallelLibrary)]
    [TestCase(30, 1, 1, WorkerFactory.DedicatedWorkerThreads)]
    [TestCase(30, 1, 1, WorkerFactory.TaskParallelLibrary)]
    [TestCase(3000, 1, 1, WorkerFactory.DedicatedWorkerThreads)]
    [TestCase(3000, 1, 1, WorkerFactory.TaskParallelLibrary)]
    [TestCase(30000, 1, 1, WorkerFactory.DedicatedWorkerThreads)]
    [TestCase(30000, 1, 1, WorkerFactory.TaskParallelLibrary)]
    [TestCase(300000, 1, 1, WorkerFactory.DedicatedWorkerThreads)]
    [TestCase(300000, 1, 1, WorkerFactory.TaskParallelLibrary)]
    public async Task CheckThreads(int messageCount, int workers, int parallelism, WorkerFactory workerFactory)
    {
        var (activator, starter) = CreateBus(workers, parallelism, workerFactory);

        var counter = new SharedCounter(messageCount);
        activator.Handle<string>(async str => counter.Decrement());

        var bus = starter.Start();

        await Task.WhenAll(Enumerable.Range(0, messageCount)
            .Select(n => bus.SendLocal($"THIS IS MESSAGE {n}")));

        counter.WaitForResetEvent(timeoutSeconds: 15);
    }

    (BuiltinHandlerActivator, IBusStarter) CreateBus(int workers, int parallelism, WorkerFactory workerFactory)
    {
        var activator = new BuiltinHandlerActivator();

        Using(activator);

        var starter = Configure.With(activator)
            .Logging(l => l.ColoredConsole(minLevel: LogLevel.Info))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "threads"))
            .Options(o =>
            {
                o.SetNumberOfWorkers(workers);
                o.SetMaxParallelism(parallelism);

                if (workerFactory == WorkerFactory.TaskParallelLibrary)
                {
                    o.UseTplToReceiveMessages();
                }
            })
            .Create();

        return (activator, starter);
    }
}