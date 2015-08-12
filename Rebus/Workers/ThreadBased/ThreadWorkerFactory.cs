using System;
using Rebus.Pipeline;
using Rebus.Threading;
using Rebus.Transport;

namespace Rebus.Workers.ThreadBased
{
    /// <summary>
    /// Implementation of <see cref="IWorkerFactory"/> that creates <see cref="ThreadWorker"/> instances when asked for
    /// an <see cref="IWorker"/>. Each <see cref="ThreadWorker"/> has its own dedicated worker thread that performs
    /// all the work (which consists of receiving new messages and running continuations)
    /// </summary>
    public class ThreadWorkerFactory : IWorkerFactory
    {
        readonly ThreadWorkerSynchronizationContext _threadWorkerSynchronizationContext = new ThreadWorkerSynchronizationContext();
        readonly ParallelOperationsManager _parallelOperationsManager;
        readonly ITransport _transport;
        readonly IPipeline _pipeline;
        readonly IPipelineInvoker _pipelineInvoker;

        /// <summary>
        /// Constructs the worker factory
        /// </summary>
        public ThreadWorkerFactory(ITransport transport, IPipeline pipeline, IPipelineInvoker pipelineInvoker, int maxParallelism)
        {
            if (transport == null) throw new ArgumentNullException("transport");
            if (pipeline == null) throw new ArgumentNullException("pipeline");
            if (pipelineInvoker == null) throw new ArgumentNullException("pipelineInvoker");
            if (maxParallelism <= 0) throw new ArgumentOutOfRangeException(string.Format("Cannot use value '{0}' as max parallelism!", maxParallelism));
            _transport = transport;
            _pipeline = pipeline;
            _pipelineInvoker = pipelineInvoker;
            _parallelOperationsManager = new ParallelOperationsManager(maxParallelism);
        }

        /// <summary>
        /// Creates a new worker (i.e. a new thread) with the given name
        /// </summary>
        public IWorker CreateWorker(string workerName)
        {
            return new ThreadWorker(_transport, _pipeline, _pipelineInvoker, workerName, _threadWorkerSynchronizationContext, _parallelOperationsManager);
        }
    }
}