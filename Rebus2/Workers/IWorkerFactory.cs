using Rebus2.Bus;

namespace Rebus2.Workers
{
    public interface IWorkerFactory
    {
        IWorker CreateWorker(string workerName);
    }
}