using System;
using System.Linq;
using System.Threading;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Threading;
using Rebus.Transport;

namespace Rebus.Workers.ThreadBased
{
    /// <summary>
    /// Implementation of <see cref="IWorker"/> that has a dedicated thread the continuously polls the given <see cref="ThreadWorkerSynchronizationContext"/> for work,
    /// and in case it doesn't find any, it'll try to receive a new message and invoke a receive pipeline on that
    /// </summary>
    public class ThreadWorker : IWorker
    {
        static ILog _log;

        static ThreadWorker()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly BackoffHelper _backoffHelper = new BackoffHelper();
        readonly ThreadWorkerSynchronizationContext _threadWorkerSynchronizationContext;
        readonly ITransport _transport;
        readonly IPipeline _pipeline;
        readonly Thread _workerThread;
        readonly IPipelineInvoker _pipelineInvoker;
        readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        readonly ParallelOperationsManager _parallelOperationsManager;

        volatile bool _keepWorking = true;

        internal ThreadWorker(ITransport transport, IPipeline pipeline, IPipelineInvoker pipelineInvoker, string workerName, ThreadWorkerSynchronizationContext threadWorkerSynchronizationContext, ParallelOperationsManager parallelOperationsManager)
        {
            Name = workerName;

            _transport = transport;
            _pipeline = pipeline;
            _pipelineInvoker = pipelineInvoker;
            _threadWorkerSynchronizationContext = threadWorkerSynchronizationContext;
            _parallelOperationsManager = parallelOperationsManager;
            _workerThread = new Thread(ThreadStart)
            {
                Name = workerName,
                IsBackground = true
            };
            _workerThread.Start();
        }

        void ThreadStart()
        {
            SynchronizationContext.SetSynchronizationContext(_threadWorkerSynchronizationContext);

            _log.Debug("Starting (thread-based) worker {0}", Name);

            while (_keepWorking)
            {
                DoWork();
            }

            var stopTime = DateTime.UtcNow;

            if (_parallelOperationsManager.HasPendingTasks)
            {
                _log.Info("Continuations are waiting to be posted.... will wait up to 1 minute");
            }

            while (_parallelOperationsManager.HasPendingTasks)
            {
                DoWork(onlyRunContinuations: true);

                if (stopTime.ElapsedUntilNow() < TimeSpan.FromMinutes(1)) continue;

                _log.Warn("Not all async tasks were able to finish within 1 minute!!!");
                break;
            }

            _log.Debug("Worker {0} stopped", Name);
        }

        void DoWork(bool onlyRunContinuations = false)
        {
            try
            {
                var nextContinuationOrNull = _threadWorkerSynchronizationContext.GetNextContinuationOrNull();

                if (nextContinuationOrNull != null)
                {
                    nextContinuationOrNull();
                    return;
                }

                if (!onlyRunContinuations)
                {
                    TryProcessMessage();
                }
            }
            catch (ThreadAbortException)
            {
                _log.Debug("Aborting worker {0}", Name);
                _keepWorking = false;
            }
            catch (Exception exception)
            {
                _log.Error(exception, "Error while attempting to do work");
            }
        }

        async void TryProcessMessage()
        {
            using (var op = _parallelOperationsManager.TryBegin())
            {
                if (!op.CanContinue())
                {
                    Thread.Sleep(10);
                    return;
                }

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
                        _log.Error(exception, "Unhandled exception in thread worker");
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
            if (_keepWorking)
            {
                _keepWorking = false;
                _cancellationTokenSource.Cancel();
            }
        }

        public void Dispose()
        {
            Stop();

            if (!_workerThread.Join(TimeSpan.FromSeconds(5)))
            {
                _log.Warn("Worker {0} did not stop withing 5 second timeout!", Name);
            }
        }
    }
}