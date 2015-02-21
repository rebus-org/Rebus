using Rebus2.Bus;
using Rebus2.Pipeline;
using Rebus2.Transport;

namespace Rebus2.Workers
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

        public IWorker CreateWorker(string workerName)
        {
            return new ThreadWorker(_transport, _pipeline, _pipelineInvoker, workerName, _threadWorkerSynchronizationContext);
        }
    }
}