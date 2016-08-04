using System;
using System.Diagnostics;
using System.Threading;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Threading;
using Rebus.Transport;

namespace Rebus.Workers.ThreadPoolBased
{
    /// <summary>
    /// Implementation of <see cref="IWorkerFactory"/> that uses worker threads to do synchronous receive of messages, dispatching
    /// received messages to the threadpool.
    /// </summary>
    public class ThreadPoolWorkerFactory : IWorkerFactory
    {
        readonly ITransport _transport;
        readonly IRebusLoggerFactory _rebusLoggerFactory;
        readonly IPipeline _pipeline;
        readonly IPipelineInvoker _pipelineInvoker;
        readonly Options _options;
        readonly Func<RebusBus> _busGetter;
        readonly ISyncBackoffStrategy _backoffStrategy;
        readonly ParallelOperationsManager _parallelOperationsManager;
        readonly ILog _log;

        /// <summary>
        /// Creates the worker factory
        /// </summary>
        public ThreadPoolWorkerFactory(ITransport transport, IRebusLoggerFactory rebusLoggerFactory, IPipeline pipeline, IPipelineInvoker pipelineInvoker, Options options, Func<RebusBus> busGetter, BusLifetimeEvents busLifetimeEvents, ISyncBackoffStrategy backoffStrategy)
        {
            if (transport == null) throw new ArgumentNullException(nameof(transport));
            if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));
            if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));
            if (pipelineInvoker == null) throw new ArgumentNullException(nameof(pipelineInvoker));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (busGetter == null) throw new ArgumentNullException(nameof(busGetter));
            if (busLifetimeEvents == null) throw new ArgumentNullException(nameof(busLifetimeEvents));
            if (backoffStrategy == null) throw new ArgumentNullException(nameof(backoffStrategy));
            _transport = transport;
            _rebusLoggerFactory = rebusLoggerFactory;
            _pipeline = pipeline;
            _pipelineInvoker = pipelineInvoker;
            _options = options;
            _busGetter = busGetter;
            _backoffStrategy = backoffStrategy;
            _parallelOperationsManager = new ParallelOperationsManager(options.MaxParallelism);
            _log = _rebusLoggerFactory.GetCurrentClassLogger();

            if (_options.MaxParallelism < 1)
            {
                throw new ArgumentException($"Max parallelism is {_options.MaxParallelism} which is an invalid value");
            }

            if (options.WorkerShutdownTimeout < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException($"Cannot use '{options.WorkerShutdownTimeout}' as worker shutdown timeout as it");
            }

            busLifetimeEvents.WorkersStopped += WaitForContinuationsToFinish;
        }

        /// <summary>
        /// Creates a new worker with the given <paramref name="workerName"/>
        /// </summary>
        public IWorker CreateWorker(string workerName)
        {
            if (workerName == null) throw new ArgumentNullException(nameof(workerName));

            var owningBus = _busGetter();

            var worker = new ThreadPoolWorker(workerName, _transport, _rebusLoggerFactory, _pipeline, _pipelineInvoker, _parallelOperationsManager, owningBus, _options, _backoffStrategy);

            return worker;
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
                    _log.Warn("Not all async tasks were able to finish within given timeout of {0} seconds!", workerShutdownTimeout.TotalSeconds);
                    break;
                }
            }
        }
    }
}