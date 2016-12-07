using System;
using System.Threading;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;
using Rebus.Workers;
using Xunit;

namespace Rebus.Tests.Integration
{
    public class TestWorkerAndParallelismConfiguration : FixtureBase
    {
        [Fact]
        public void NumberOfWorkersIsLimitedByMaxParallelism()
        {
            var counter = new WorkerCounter();

            using (var adapter = new BuiltinHandlerActivator())
            {
                Configure.With(adapter)
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "worker-/parallelism-test"))
                    .Options(o =>
                    {
                        o.SetMaxParallelism(1);
                        o.SetNumberOfWorkers(10);

                        o.Decorate<IWorkerFactory>(c =>
                        {
                            counter.SetWorkerFactory(c.Get<IWorkerFactory>());

                            return counter;
                        });
                    })
                    .Start();

                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            Assert.Equal(1,counter.NumberOfWorkersCreated);
        }

        class WorkerCounter : IWorkerFactory
        {
            IWorkerFactory _workerFactory;

            public void SetWorkerFactory(IWorkerFactory workerFactory)
            {
                _workerFactory = workerFactory;
            }

            public IWorker CreateWorker(string workerName)
            {
                NumberOfWorkersCreated++;

                return _workerFactory.CreateWorker(workerName);
            }

            public int NumberOfWorkersCreated { get; private set; }
        }
    }
}