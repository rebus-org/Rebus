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

        public ThreadWorkerFactory(ITransport transport, IPipeline pipeline)
        {
            _transport = transport;
            _pipeline = pipeline;
        }

        public IWorker CreateWorker(string workerName)
        {
            return new ThreadWorker(_transport, _pipeline, workerName, _threadWorkerSynchronizationContext);
        }
    }
}