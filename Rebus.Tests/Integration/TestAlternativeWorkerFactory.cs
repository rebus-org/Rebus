using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Tests.Extensions;
using Rebus.Threading;
using Rebus.Transport;
using Rebus.Transport.InMem;
using Rebus.Workers;
using Rebus.Workers.ThreadBased;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestAlternativeWorkerFactory : FixtureBase
    {
        [Test]
        public async Task NizzleName()
        {
            var gotMessage = new ManualResetEvent(false);

            using (var activator = new BuiltinHandlerActivator())
            {
                activator.Handle<string>(async s =>
                {
                    Console.WriteLine("Got message: {0}", s);
                    gotMessage.Set();
                });

                Configure.With(activator)
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "bimse"))
                    .Options(o =>
                    {
                        o.Register<IWorkerFactory>(c => new AsyncTaskWorkerFactory(c.Get<ITransport>(), c.Get<IPipeline>(), c.Get<IPipelineInvoker>()));
                    })
                    .Start();

                await activator.Bus.SendLocal("hej med dig min ven");

                gotMessage.WaitOrDie(TimeSpan.FromSeconds(3));
            }
        }

        [Test]
        public async Task CanReceiveBunchOfMessages()
        {
            var events = new ConcurrentQueue<string>();

            using (var activator = new BuiltinHandlerActivator())
            {
                activator.Handle<string>(async s => events.Enqueue(s));

                Configure.With(activator)
                    .Logging(l => l.Console(minLevel:LogLevel.Info))
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "bimse"))
                    .Options(o =>
                    {
                        o.Register<IWorkerFactory>(c => new AsyncTaskWorkerFactory(c.Get<ITransport>(), c.Get<IPipeline>(), c.Get<IPipelineInvoker>()));
                        o.SetNumberOfWorkers(100);
                    })
                    .Start();

                var bus = activator.Bus;

                await Task.WhenAll(Enumerable.Range(0, 100)
                    .Select(i => bus.SendLocal(string.Format("msg-{0}", i))));

                await Task.Delay(1000);

                Assert.That(events.Count, Is.EqualTo(100));
            }
        }

        class AsyncTaskWorkerFactory : IWorkerFactory
        {
            readonly ITransport _transport;
            readonly IPipeline _pipeline;
            readonly IPipelineInvoker _pipelineInvoker;

            public AsyncTaskWorkerFactory(ITransport transport, IPipeline pipeline, IPipelineInvoker pipelineInvoker)
            {
                _transport = transport;
                _pipeline = pipeline;
                _pipelineInvoker = pipelineInvoker;
            }

            public IWorker CreateWorker(string workerName)
            {
                return new AsyncTaskWorker(workerName, _transport, _pipeline, _pipelineInvoker, 10);
            }
        }

        class AsyncTaskWorker : IWorker
        {
            static ILog _log;

            static AsyncTaskWorker()
            {
                RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
            }

            readonly BackoffHelper _backoffHelper = new BackoffHelper();
            readonly ParallelOperationsManager _parallelOperationsManager;
            readonly ITransport _transport;
            readonly IPipeline _pipeline;
            readonly IPipelineInvoker _pipelineInvoker;
            readonly AsyncTask _workerTask;

            bool _workerStopped;

            public AsyncTaskWorker(string name, ITransport transport, IPipeline pipeline, IPipelineInvoker pipelineInvoker, int maxParallelismPerWorker)
            {
                _transport = transport;
                _pipeline = pipeline;
                _pipelineInvoker = pipelineInvoker;
                _parallelOperationsManager = new ParallelOperationsManager(maxParallelismPerWorker);

                Name = name;

                _workerTask = new AsyncTask(name, DoWork, prettyInsignificant: true)
                {
                    Interval = TimeSpan.FromMilliseconds(1)
                };
                _log.Debug("Starting (task-based) worker {0}", Name);
                _workerTask.Start();
            }

            async Task DoWork()
            {
                using (var op = _parallelOperationsManager.TryBegin())
                {
                    if (!op.CanContinue()) return;

                    using (var transactionContext = new DefaultTransactionContext())
                    {
                        AmbientTransactionContext.Current = transactionContext;
                        try
                        {
                            var message = await _transport.Receive(transactionContext);

                            if (message == null)
                            {
                                // finish the tx and wait....
                                await transactionContext.Complete();
                                await _backoffHelper.Wait();
                                return;
                            }

                            _backoffHelper.Reset();

                            var context = new IncomingStepContext(message, transactionContext);
                            transactionContext.Items[StepContext.StepContextKey] = context;

                            var stagedReceiveSteps = _pipeline.ReceivePipeline();

                            await _pipelineInvoker.Invoke(context, stagedReceiveSteps);

                            await transactionContext.Complete();
                        }
                        catch (Exception exception)
                        {
                            _log.Error(exception, "Unhandled exception in task worker");
                        }
                        finally
                        {
                            AmbientTransactionContext.Current = null;
                        }
                    }
                }
            }

            public string Name { get; private set; }

            public void Stop()
            {
                DisposeTask();
            }

            public void Dispose()
            {
                DisposeTask();
            }

            void DisposeTask()
            {
                if (_workerStopped) return;

                try
                {
                    _workerTask.Dispose();
                    _log.Debug("Worker {0} stopped", Name);
                }
                finally
                {
                    _workerStopped = true;
                }
            }
        }
    }
}