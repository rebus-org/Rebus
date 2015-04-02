using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.Workers.ThreadBased
{
    public class ThreadWorkerFactory : IWorkerFactory
    {
        readonly ThreadWorkerSynchronizationContext _threadWorkerSynchronizationContext = new ThreadWorkerSynchronizationContext();
        readonly ITransport _transport;
        readonly IPipeline _pipeline;
        readonly IPipelineInvoker _pipelineInvoker;

        public ThreadWorkerFactory(ITransport transport, IPipeline pipeline, IPipelineInvoker pipelineInvoker)
        {
            _transport = transport;
            _pipeline = pipeline;
            _pipelineInvoker = pipelineInvoker;
        }

        public int MaxParallelismPerWorker { get; set; }

        public IWorker CreateWorker(string workerName)
        {
            return new ThreadWorker(_transport, _pipeline, _pipelineInvoker, workerName, _threadWorkerSynchronizationContext, MaxParallelismPerWorker);
        }
    }
}