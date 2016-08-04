using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Threading;
using Rebus.Transport;

namespace Rebus.Workers.ThreadPoolBased
{
    class ThreadPoolWorker : IWorker
    {
        readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        readonly ITransport _transport;
        readonly IPipeline _pipeline;
        readonly IPipelineInvoker _pipelineInvoker;
        readonly ParallelOperationsManager _parallelOperationsManager;
        readonly RebusBus _owningBus;
        readonly Options _options;
        readonly Thread _workerThread;
        readonly ILog _log;

        internal ThreadPoolWorker(string name, ITransport transport, IRebusLoggerFactory rebusLoggerFactory, IPipeline pipeline, IPipelineInvoker pipelineInvoker, ParallelOperationsManager parallelOperationsManager, RebusBus owningBus, Options options)
        {
            Name = name;
            _log = rebusLoggerFactory.GetCurrentClassLogger();
            _transport = transport;
            _pipeline = pipeline;
            _pipelineInvoker = pipelineInvoker;
            _parallelOperationsManager = parallelOperationsManager;
            _owningBus = owningBus;
            _options = options;
            _workerThread = new Thread(Run)
            {
                Name = name,
                IsBackground = true
            };
            _workerThread.Start();
        }

        public string Name { get; }

        void Run()
        {
            _log.Debug("Starting (threadpool-based) worker {0}", Name);

            var token = _cancellationTokenSource.Token;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    TryReceiveNextMessage(token);
                }
                catch (TaskCanceledException)
                {
                    // it's fine
                }
                catch (OperationCanceledException)
                {
                    // it's fine
                }
                catch (Exception exception)
                {
                    _log.Error(exception, "Unhandled exception in worker!!");
                }
            }

            _log.Debug("Worker {0} stopped", Name);
        }

        void TryReceiveNextMessage(CancellationToken token)
        {
            var parallelOperation = _parallelOperationsManager.TryBegin();

            if (!parallelOperation.CanContinue()) return;

            try
            {
                var context = new DefaultTransactionContext();
                var transportMessage = _transport.Receive(context, token).Result;

                if (transportMessage == null)
                {
                    context.Dispose();
                    parallelOperation.Dispose();
                    Thread.Sleep(20);
                    return;
                }

                // fire asynchronously to the thread pool! (disable warning because it is intentionally NOT waiting for it to finish)
                ProcessMessage(context, transportMessage, parallelOperation, token);
            }
            catch (AggregateException aggregateException)
            {
                parallelOperation.Dispose();

                var baseException = aggregateException.GetBaseException();

                if (baseException is TaskCanceledException || baseException is OperationCanceledException)
                {
                    var info = ExceptionDispatchInfo.Capture(baseException);

                    info.Throw();
                }

                throw;
            }
            catch
            {
                parallelOperation.Dispose();
                throw;
            }
        }

        /// <summary>
        /// This bad boy is async void because we fire it asynchronously to the thread pool. We keep tight control over it via the passed-in <paramref name="parallelOperation"/>
        /// </summary>
        async void ProcessMessage(DefaultTransactionContext context, TransportMessage transportMessage, IDisposable parallelOperation, CancellationToken token)
        {
            using (parallelOperation)
            using (context)
            {
                try
                {
                    context.Items["CancellationToken"] = token;
                    context.Items["OwningBus"] = _owningBus;
                    AmbientTransactionContext.Current = context;

                    var incomingSteps = _pipeline.ReceivePipeline();
                    var stepContext = new IncomingStepContext(transportMessage, context);
                    await _pipelineInvoker.Invoke(stepContext, incomingSteps);

                    try
                    {
                        await context.Complete();
                    }
                    catch (Exception exception)
                    {
                        _log.Error(exception, "An error occurred when attempting to complete the transaction context");
                    }
                }
                catch (ThreadAbortException exception)
                {
                    context.Abort();

                    _log.Error(exception, $"Worker was killed while handling message {transportMessage.GetMessageLabel()}");
                }
                catch (Exception exception)
                {
                    context.Abort();

                    _log.Error(exception, $"Unhandled exception while handling message {transportMessage.GetMessageLabel()}");
                }
                finally
                {
                    AmbientTransactionContext.Current = null;
                }
            }
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            Stop();

            if (!_workerThread.Join(_options.WorkerShutdownTimeout))
            {
                _log.Warn($"The '{Name}' worker did not shut down within {_options.WorkerShutdownTimeout.TotalSeconds} seconds!");

                _workerThread.Abort();
            }
        }
    }
}