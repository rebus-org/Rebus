using Rebus.Pipeline;
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
        readonly ITransport _transport;
        readonly IPipeline _pipeline;
        readonly IPipelineInvoker _pipelineInvoker;

        /// <summary>
        /// Constructs the worker factory
        /// </summary>
        public ThreadWorkerFactory(ITransport transport, IPipeline pipeline, IPipelineInvoker pipelineInvoker)
        {
            _transport = transport;
            _pipeline = pipeline;
            _pipelineInvoker = pipelineInvoker;
        }

        /// <summary>
        /// Configures the degree of parallelism allowed within each worker, i.e. how many concurrent operations one single
        /// worker thread can perform, using async/await
        /// </summary>
        public int MaxParallelismPerWorker { get; set; }

        public IWorker CreateWorker(string workerName)
        {
            return new ThreadWorker(_transport, _pipeline, _pipelineInvoker, workerName, _threadWorkerSynchronizationContext, MaxParallelismPerWorker);
        }
    }
}