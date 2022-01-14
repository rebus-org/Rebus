using System;
using System.Diagnostics;
using System.Threading;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Threading;
using Rebus.Transport;
using Rebus.Workers.ThreadPoolBased;

namespace Rebus.Workers.TplBased;

/// <summary>
/// Implementation of <see cref="IWorkerFactory"/> that uses Task Parallel Library to receive messages.
/// Must only be used with truly async transports (i.e. transports capable of doing non-blocking async
/// receive operations), otherwise the thread pool might be robbed of its threads
/// </summary>
public class TplWorkerFactory : IWorkerFactory
{
    readonly ParallelOperationsManager _parallelOperationsManager;
    readonly CancellationToken _busDisposalCancellationToken;
    readonly IRebusLoggerFactory _rebusLoggerFactory;
    readonly IBackoffStrategy _backoffStrategy;
    readonly IPipelineInvoker _pipelineInvoker;
    readonly Func<RebusBus> _busGetter;
    readonly ITransport _transport;
    readonly Options _options;
    readonly ILog _log;

    /// <summary>
    /// Constructs the TPL worker factory
    /// </summary>
    public TplWorkerFactory(ITransport transport, IRebusLoggerFactory rebusLoggerFactory, IPipelineInvoker pipelineInvoker, Options options, Func<RebusBus> busGetter, BusLifetimeEvents busLifetimeEvents, IBackoffStrategy backoffStrategy, CancellationToken busDisposalCancellationToken)
    {
        _transport = transport;
        _rebusLoggerFactory = rebusLoggerFactory;
        _pipelineInvoker = pipelineInvoker;
        _options = options;
        _busGetter = busGetter;
        _backoffStrategy = backoffStrategy;
        _busDisposalCancellationToken = busDisposalCancellationToken;
        _parallelOperationsManager = new ParallelOperationsManager(options.MaxParallelism);
        _log = _rebusLoggerFactory.GetLogger<TplWorkerFactory>();

        busLifetimeEvents.WorkersStopped += WaitForContinuationsToFinish;
    }

    /// <summary>
    /// Blocks until all work has finished being done (i.e. waits for all message handling continuations to have been executed)
    /// </summary>
    void WaitForContinuationsToFinish()
    {
        if (!_parallelOperationsManager.HasPendingTasks) return;

        // give quick chance to finish working without logging anything
        Thread.Sleep(100);

        if (!_parallelOperationsManager.HasPendingTasks) return;

        // let the world know that we are waiting for something to finish
        _log.Info("Waiting for message handler continuations to finish...");

        var stopwatch = Stopwatch.StartNew();
        var workerShutdownTimeout = _options.WorkerShutdownTimeout;

        while (true)
        {
            Thread.Sleep(100);

            if (!_parallelOperationsManager.HasPendingTasks)
            {
                _log.Info("Done :)");
                break;
            }

            if (stopwatch.Elapsed > workerShutdownTimeout)
            {
                _log.Warn("Not all async tasks were able to finish within given timeout of {timeoutSeconds} seconds!", workerShutdownTimeout.TotalSeconds);
                break;
            }
        }
    }

    /// <summary>
    /// Creates a new "worker thread"
    /// </summary>
    public IWorker CreateWorker(string workerName)
    {
        if (workerName == null) throw new ArgumentNullException(nameof(workerName));

        var owningBus = _busGetter();

        return new TplWorker(
            workerName,
            owningBus,
            _transport,
            _rebusLoggerFactory,
            _pipelineInvoker,
            _parallelOperationsManager,
            _options,
            _backoffStrategy,
            _busDisposalCancellationToken
        );
    }
}