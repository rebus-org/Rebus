namespace Rebus.Bus.Advanced
{
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
        /// Adds a new worker to the collection of workers. Beware that there's no ceiling on this operation, and adding many workers can severely impact performance.
        /// </summary>
        void AddWorker();

        /// <summary>
        /// Removes a worker from the collection of workers. Ignores the call if there's zero workers.
        /// </summary>
        void RemoveWorker();
    }
}