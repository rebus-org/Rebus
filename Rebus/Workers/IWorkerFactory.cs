using Rebus.Bus;

namespace Rebus.Workers
{
    public interface IWorkerFactory
    {
        IWorker CreateWorker(string workerName);
    }
}