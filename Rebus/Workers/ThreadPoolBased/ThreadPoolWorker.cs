using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Threading;
using Rebus.Transport;
// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace Rebus.Workers.ThreadPoolBased;

class ThreadPoolWorker : IWorker
{
    readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    readonly ManualResetEvent _workerShutDown = new ManualResetEvent(false);
    readonly ITransport _transport;
    readonly IPipelineInvoker _pipelineInvoker;
    readonly ParallelOperationsManager _parallelOperationsManager;
    readonly RebusBus _owningBus;
    readonly Options _options;
    readonly IBackoffStrategy _backoffStrategy;
    readonly CancellationToken _busDisposalCancellationToken;
    readonly Thread _workerThread;
    readonly ILog _log;

    internal ThreadPoolWorker(string name, ITransport transport, IRebusLoggerFactory rebusLoggerFactory,
        IPipelineInvoker pipelineInvoker, ParallelOperationsManager parallelOperationsManager, RebusBus owningBus,
        Options options, IBackoffStrategy backoffStrategy, CancellationToken busDisposalCancellationToken)
    {
        Name = name;
        _log = rebusLoggerFactory.GetLogger<ThreadPoolWorker>();
        _transport = transport;
        _pipelineInvoker = pipelineInvoker;
        _parallelOperationsManager = parallelOperationsManager;
        _owningBus = owningBus;
        _options = options;
        _backoffStrategy = backoffStrategy;
        _busDisposalCancellationToken = busDisposalCancellationToken;
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
        _log.Debug("Starting (threadpool-based) worker {workerName}", Name);

        var token = _cancellationTokenSource.Token;

        while (!token.IsCancellationRequested)
        {
            try
            {
                TryReceiveNextMessage(token);
            }
            catch (Exception exception)
            {
                _log.Error(exception, "Unhandled exception in worker {workerName} when try-receiving", Name);

                _backoffStrategy.WaitError(token);
            }
        }

        _log.Debug("Worker {workerName} stopped", Name);

        _workerShutDown.Set();
    }

    void TryReceiveNextMessage(CancellationToken token)
    {
        var parallelOperation = _parallelOperationsManager.TryBegin();

        if (!parallelOperation.CanContinue())
        {
            _backoffStrategy.Wait(token);
            return;
        }

        TryAsyncReceive(token, parallelOperation)
            .ContinueWith(LogException, TaskContinuationOptions.OnlyOnFaulted);
    }

    void LogException(Task task)
    {
        var exception = task.Exception;
        if (exception == null) return;
        _log.Error(exception, "Unhandled exception in worker {workerName}", Name);
    }

    async Task TryAsyncReceive(CancellationToken token, IDisposable parallelOperation)
    {
        try
        {
            using (parallelOperation)
            using (var context = new TransactionContextWithOwningBus(_owningBus))
            {
                var transportMessage = await ReceiveTransportMessage(token, context);

                if (transportMessage == null)
                {
                    context.Dispose();

                    // get out quickly if we're shutting down
                    if (token.IsCancellationRequested) return;

                    // no need for another thread to rush in and discover that there is no message
                    //parallelOperation.Dispose();

                    await _backoffStrategy.WaitNoMessageAsync(token);
                    return;
                }

                _backoffStrategy.Reset();

                try
                {
                    AmbientTransactionContext.SetCurrent(context);
                    await ProcessMessage(context, transportMessage);
                }
                finally
                {
                    AmbientTransactionContext.SetCurrent(null);
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested || _busDisposalCancellationToken.IsCancellationRequested)
        {
            // it's fine - just a sign that we are shutting down
        }
        catch (Exception exception)
        {
            _log.Error(exception, "Unhandled exception in worker {workerName}", Name);
        }
    }

    async Task<TransportMessage> ReceiveTransportMessage(CancellationToken token, ITransactionContext context)
    {
        try
        {
            return await _transport.Receive(context, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested || _busDisposalCancellationToken.IsCancellationRequested)
        {
            // it's fine - just a sign that we are shutting down
            return null;
        }
        catch (Exception exception)
        {
            _log.Warn("An error occurred when attempting to receive the next message: {exception}", exception);

            await _backoffStrategy.WaitErrorAsync(token);

            return null;
        }
    }

    async Task ProcessMessage(TransactionContext context, TransportMessage transportMessage)
    {
        try
        {
            var stepContext = new IncomingStepContext(transportMessage, context);

            stepContext.Save(_busDisposalCancellationToken);

            await _pipelineInvoker.Invoke(stepContext);

            try
            {
                await context.Complete();
            }
            catch (Exception exception)
            {
                _log.Error(exception, "An error occurred when attempting to complete the transaction context");
            }
        }
        catch (OperationCanceledException exception)
        {
            _log.Error(exception, "Worker was aborted while handling message {messageLabel}", transportMessage.GetMessageLabel());
        }
        catch (Exception exception)
        {
            _log.Error(exception, "Unhandled exception while handling message {messageLabel}", transportMessage.GetMessageLabel());
        }
    }

    public void Stop() => _cancellationTokenSource.Cancel();

    public void Dispose()
    {
        Stop();

        if (!_workerShutDown.WaitOne(_options.WorkerShutdownTimeout))
        {
            _log.Warn("The {workerName} worker did not shut down within {shutdownTimeoutSeconds} seconds!",
                Name, _options.WorkerShutdownTimeout.TotalSeconds);
        }
    }
}