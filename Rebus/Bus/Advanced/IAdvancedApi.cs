namespace Rebus.Bus.Advanced
{
    /// <summary>
    /// API for the advanced features of the bus
    /// </summary>
    public interface IAdvancedApi
    {
        /// <summary>
        /// Gets an API to query/control various aspects around Rebus' workers
        /// </summary>
        IWorkersApi Workers { get; }

        /// <summary>
        /// Gets an API to do pub/sub on raw string-based topics
        /// </summary>
        ITopicsApi Topics { get; }
    }
}