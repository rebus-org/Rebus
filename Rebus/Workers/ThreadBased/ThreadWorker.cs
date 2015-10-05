using System;
using System.Threading;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Threading;
using Rebus.Time;
using Rebus.Transport;

namespace Rebus.Workers.ThreadBased
{
    /// <summary>
    /// Implementation of <see cref="IWorker"/> that has a dedicated thread the continuously polls the given <see cref="ThreadWorkerSynchronizationContext"/> for work,
    /// and in case it doesn't find any, it'll try to receive a new message and invoke a receive pipeline on that
    /// </summary>
    public class ThreadWorker : IWorker
    {
        readonly ThreadWorkerSynchronizationContext _threadWorkerSynchronizationContext;
        readonly IBackoffStrategy _backoffStrategy;
        readonly ITransport _transport;
        readonly IPipeline _pipeline;
        readonly Thread _workerThread;
        readonly IPipelineInvoker _pipelineInvoker;
        readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        readonly ParallelOperationsManager _parallelOperationsManager;
        readonly ILog _log;

        volatile bool _keepWorking = true;
        bool _disposed;

        internal ThreadWorker(ITransport transport, IPipeline pipeline, IPipelineInvoker pipelineInvoker, string workerName, ThreadWorkerSynchronizationContext threadWorkerSynchronizationContext, ParallelOperationsManager parallelOperationsManager, IBackoffStrategy backoffStrategy, IRebusLoggerFactory rebusLoggerFactory)
        {
            Name = workerName;

            _log = rebusLoggerFactory.GetCurrentClassLogger();
            _transport = transport;
            _pipeline = pipeline;
            _pipelineInvoker = pipelineInvoker;
            _threadWorkerSynchronizationContext = threadWorkerSynchronizationContext;
            _parallelOperationsManager = parallelOperationsManager;
            _backoffStrategy = backoffStrategy;
            _workerThread = new Thread(ThreadStart)
            {
                Name = workerName,
                IsBackground = true
            };
            _workerThread.Start();
        }

        /// <summary>
        /// Disposes any unmanaged held resources
        /// </summary>
        ~ThreadWorker()
        {
            Dispose(false);
        }

        void ThreadStart()
        {
            SynchronizationContext.SetSynchronizationContext(_threadWorkerSynchronizationContext);

            _log.Debug("Starting (thread-based) worker {0}", Name);

            while (_keepWorking)
            {
                DoWork();
            }

            var stopTime = RebusTime.Now;

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

                // if there's a continuation to run, run it
                if (nextContinuationOrNull != null)
                {
                    nextContinuationOrNull();
                    return;
                }

                var canTryToReceiveNewMessage = !onlyRunContinuations;

                if (canTryToReceiveNewMessage)
                {
                    TryReceiveNewMessage();
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

        async void TryReceiveNewMessage()
        {
            using (var operation = _parallelOperationsManager.TryBegin())
            {
                // if we didn't get to do our thing, pause the thread a very short while to avoid thrashing too much
                if (!operation.CanContinue())
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
                            // no message: finish the tx and wait....
                            await transactionContext.Complete();
                            await _backoffStrategy.Wait();
                            return;
                        }

                        // we got a message, so we reset the backoff strategy
                        _backoffStrategy.Reset();

                        var context = new IncomingStepContext(message, transactionContext);
                        
                        var stagedReceiveSteps = _pipeline.ReceivePipeline();
                        
                        await _pipelineInvoker.Invoke(context, stagedReceiveSteps);
                        
                        await transactionContext.Complete();
                    }
                    catch (Exception exception)
                    {
                        // we should not end up here unless something is off....
                        _log.Error(exception, "Unhandled exception in thread worker - the pipeline didn't handle its own errors (this is bad)");
                        Thread.Sleep(100);
                    }
                    finally
                    {
                        AmbientTransactionContext.Current = null;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the name of this thread worker
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Stops this thread worker
        /// </summary>
        public void Stop()
        {
            if (!_keepWorking) return;

            _keepWorking = false;
            _cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Stops the thread worker, waiting for it to finish whatever it was doing (up to 5 seconds)
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Ensures that the worker thread is stopped and waits for it to exit
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            try
            {
                if (disposing)
                {
                    Stop();

                    if (!_workerThread.Join(TimeSpan.FromSeconds(5)))
                    {
                        _log.Warn("Worker {0} did not stop withing 5 second timeout!", Name);
                    }
                }
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}