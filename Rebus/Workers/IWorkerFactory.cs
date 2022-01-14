namespace Rebus.Workers;

/// <summary>
/// Factory responsible for creating a "worker"
/// </summary>
public interface IWorkerFactory
{
    /// <summary>
    /// Must create a new worker with the given name
    /// </summary>
    IWorker CreateWorker(string workerName);
}