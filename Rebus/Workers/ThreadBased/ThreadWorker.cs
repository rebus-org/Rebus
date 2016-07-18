using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
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
        readonly TimeSpan _workerShutdownTimeout;
        readonly RebusBus _owningBus;

        volatile bool _keepWorking = true;
        bool _disposed;

        internal ThreadWorker(ITransport transport, IPipeline pipeline, IPipelineInvoker pipelineInvoker, string workerName, ThreadWorkerSynchronizationContext threadWorkerSynchronizationContext, ParallelOperationsManager parallelOperationsManager, IBackoffStrategy backoffStrategy, IRebusLoggerFactory rebusLoggerFactory, TimeSpan workerShutdownTimeout, RebusBus owningBus)
        {
            Name = workerName;

            _log = rebusLoggerFactory.GetCurrentClassLogger();
            _transport = transport;
            _pipeline = pipeline;
            _pipelineInvoker = pipelineInvoker;
            _threadWorkerSynchronizationContext = threadWorkerSynchronizationContext;
            _parallelOperationsManager = parallelOperationsManager;
            _backoffStrategy = backoffStrategy;
            _workerShutdownTimeout = workerShutdownTimeout;
            _owningBus = owningBus;
            _workerThread = new Thread(ThreadStart)
            {
                Name = workerName,
                IsBackground = true,
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

            var stopTime = RebusTime.Now;

            _log.Debug("Stopping (thread-based) worker {0} and waiting for it to finish pending tasks. (max {1} seconds)", Name, _workerShutdownTimeout.TotalSeconds);

            while (_parallelOperationsManager.HasPendingTasks)
            {
                DoWork(onlyRunContinuations: true);

                if (stopTime.ElapsedUntilNow() < _workerShutdownTimeout) continue;

                _log.Warn("Not all async tasks were able to finish within given timeout of {0} seconds!", _workerShutdownTimeout.TotalSeconds);
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
                // if we didn't get to do our thing, let the OS decide what to do next.... we don't hog the processor
                if (!operation.CanContinue())
                {
                    Thread.Yield();
                    return;
                }

                using (var transactionContext = new DefaultTransactionContext())
                {
                    transactionContext.Items["CancellationToken"] = _cancellationTokenSource.Token;
                    transactionContext.Items["OwningBus"] = _owningBus;
                    AmbientTransactionContext.Current = transactionContext;
                    try
                    {
                        var result = await TryReceiveTransportMessage(transactionContext, _cancellationTokenSource.Token);

                        if (result.Exception != null)
                        {
                            if (result.Exception is TaskCanceledException || result.Exception is OperationCanceledException)
                            {
                                // this is normal - we're being shut down so we just return quickly
                                transactionContext.Dispose();
                                return;
                            }

                            _log.Warn("An error occurred when attempting to receive transport message: {0}", result.Exception);

                            // error: finish the tx and wait....
                            transactionContext.Dispose();

                            await _backoffStrategy.WaitError();
                            return;
                        }

                        var message = result.TransportMessage;
                        if (message == null)
                        {
                            // no message: finish the tx and wait....
                            await transactionContext.Complete();
                            transactionContext.Dispose();

                            await _backoffStrategy.Wait();
                            return;
                        }

                        // we got a message, so we reset the backoff strategy
                        _backoffStrategy.Reset();

                        var context = new IncomingStepContext(message, transactionContext);
                        var stagedReceiveSteps = _pipeline.ReceivePipeline();

                        await _pipelineInvoker.Invoke(context, stagedReceiveSteps);

                        try
                        {
                            await transactionContext.Complete();
                        }
                        catch (Exception exception)
                        {
                            _log.Error(exception, "An error occurred when attempting to complete the transaction context");
                        }
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

        async Task<ReceiveResult> TryReceiveTransportMessage(DefaultTransactionContext transactionContext, CancellationToken cToken)
        {
            try
            {
                var message = await _transport.Receive(transactionContext, cToken);

                return new ReceiveResult(message);
            }
            //catch (TaskCanceledException tex)
            //{
            //    return new ReceiveResult(tex, true);
            //}
            catch (Exception exception)
            {
                return new ReceiveResult(exception);
            }
        }

        class ReceiveResult
        {
            public TransportMessage TransportMessage { get; }
            public Exception Exception { get; }

            public ReceiveResult(Exception exception)
            {
                Exception = exception;
            }

            public ReceiveResult(TransportMessage transportMessage)
            {
                TransportMessage = transportMessage;
            }
        }

        /// <summary>
        /// Gets the name of this thread worker
        /// </summary>
        public string Name { get; }

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
            if (_disposed) return;

            try
            {
                Stop();

                if (!_workerThread.Join(_workerShutdownTimeout))
                {
                    _log.Warn("Worker {0} did not stop withing {1} seconds timeout!", Name, _workerShutdownTimeout.TotalSeconds);
                }
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}