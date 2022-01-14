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
using Rebus.Workers.ThreadPoolBased;

namespace Rebus.Workers.TplBased;

class TplWorker : IWorker
{
    readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    readonly ManualResetEvent _workerStopped = new ManualResetEvent(false);
    readonly ParallelOperationsManager _parallelOperationsManager;
    readonly CancellationToken _busDisposalCancellationToken;
    readonly CancellationToken _cancellationToken;
    readonly IPipelineInvoker _pipelineInvoker;
    readonly IBackoffStrategy _backoffStrategy;
    readonly ITransport _transport;
    readonly RebusBus _owningBus;
    readonly Options _options;
    readonly ILog _log;

    public TplWorker(string workerName, RebusBus owningBus, ITransport transport,
        IRebusLoggerFactory rebusLoggerFactory, IPipelineInvoker pipelineInvoker,
        ParallelOperationsManager parallelOperationsManager, Options options, IBackoffStrategy backoffStrategy,
        CancellationToken busDisposalCancellationToken)
    {
        _owningBus = owningBus;
        _transport = transport;
        _pipelineInvoker = pipelineInvoker;
        _parallelOperationsManager = parallelOperationsManager;
        _options = options;
        _backoffStrategy = backoffStrategy;
        _busDisposalCancellationToken = busDisposalCancellationToken;
        Name = workerName;

        _cancellationToken = _cancellationTokenSource.Token;
        _log = rebusLoggerFactory.GetLogger<TplWorker>();

        Task.Run(Run);
    }

    async Task Run()
    {
        _log.Debug("Starting (tpl-based) worker {workerName}", Name);

        while (true)
        {
            if (_cancellationToken.IsCancellationRequested) break;
            if (_busDisposalCancellationToken.IsCancellationRequested) break;

            try
            {
                await TryProcessNextMessage();
            }
            catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested || _busDisposalCancellationToken.IsCancellationRequested)
            {
                // we're shutting down
            }
            catch (Exception exception)
            {
                _log.Error(exception, "Unhandled exception in worker {workerName}", Name);
            }
        }

        _log.Debug("Worker {workerName} stopped", Name);

        _workerStopped.Set();
    }

    async Task TryProcessNextMessage()
    {
        var parallelOperation = _parallelOperationsManager.TryBegin();

        if (!parallelOperation.CanContinue())
        {
            await _backoffStrategy.WaitAsync(_cancellationToken);
            return;
        }

        try
        {
            using (parallelOperation)
            using (var context = new TransactionContextWithOwningBus(_owningBus))
            {
                var transportMessage = await ReceiveTransportMessage(_cancellationToken, context);

                if (transportMessage == null)
                {
                    context.Dispose();

                    // get out quickly if we're shutting down
                    if (_cancellationToken.IsCancellationRequested || _busDisposalCancellationToken.IsCancellationRequested) return;

                    // no need for another thread to rush in and discover that there is no message
                    //parallelOperation.Dispose();

                    await _backoffStrategy.WaitNoMessageAsync(_cancellationToken);
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
        catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested || _busDisposalCancellationToken.IsCancellationRequested)
        {
            // we're shutting down
        }
        catch (Exception exception)
        {
            _log.Error(exception, "Unhandled exception in worker {workerName}", Name);
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
            context.Abort();

            _log.Error(exception, "Worker was aborted while handling message {messageLabel}", transportMessage.GetMessageLabel());
        }
        catch (Exception exception)
        {
            context.Abort();

            _log.Error(exception, "Unhandled exception while handling message {messageLabel}", transportMessage.GetMessageLabel());
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


    public string Name { get; }

    public void Stop() => _cancellationTokenSource.Cancel();

    public void Dispose()
    {
        Stop();

        if (!_workerStopped.WaitOne(_options.WorkerShutdownTimeout))
        {
            _log.Warn("The {workerName} worker did not shut down within {shutdownTimeoutSeconds} seconds!",
                Name, _options.WorkerShutdownTimeout.TotalSeconds);
        }
    }
}