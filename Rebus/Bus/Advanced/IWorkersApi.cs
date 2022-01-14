namespace Rebus.Bus.Advanced;

/// <summary>
/// Defines an API for working with workers
/// </summary>
public interface IWorkersApi
{
    /// <summary>
    /// Gets how many workers are currently running
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Sets the number of workers, blocking until the desired number has been reached
    /// </summary>
    void SetNumberOfWorkers(int numberOfWorkers);
}