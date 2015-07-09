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
        readonly ParallelismCounter _parallelismCounter;

        volatile bool _keepWorking = true;
        DateTime _threadStopSignalReceived;

        internal ThreadWorker(ITransport transport, IPipeline pipeline, IPipelineInvoker pipelineInvoker, string workerName, ThreadWorkerSynchronizationContext threadWorkerSynchronizationContext, int maxParallelismPerWorker)
        {
            Name = workerName;

            _transport = transport;
            _pipeline = pipeline;
            _pipelineInvoker = pipelineInvoker;
            _threadWorkerSynchronizationContext = threadWorkerSynchronizationContext;
            _parallelismCounter = new ParallelismCounter(maxParallelismPerWorker);
            _workerThread = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(_threadWorkerSynchronizationContext);
                _log.Debug("Starting (thread-based) worker {0}", Name);
                while (_keepWorking)
                {
                    DoWork();
                }

                while (true)
                {
                    DoWork(onlyRunContinuations: true);

                    if (_threadStopSignalReceived.ElapsedUntilNow() > TimeSpan.FromSeconds(1))
                    {
                        break;
                    }
                }
                _log.Debug("Worker {0} stopped", Name);
            })
            {
                Name = workerName,
                IsBackground = true
            };
            _workerThread.Start();
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
            if (!_parallelismCounter.CanContinue())
            {
                Thread.Sleep(10);
                return;
            }

            using (_parallelismCounter.Begin())
            {
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
                _threadStopSignalReceived = DateTime.UtcNow;
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