namespace Rebus.Configuration
{
    /// <summary>
    /// Defines the worker thread back off behavior.
    /// </summary>
    public enum BackoffBehavior
    {
        /// <summary>
        /// Default behavior which waits longer and longer on each empty message received.
        /// </summary>
        /// <remarks>
        /// Waits a maximum of 5 seconds.
        /// </remarks>
        Default,

        /// <summary>
        /// Low latency behavior which waits a constant time on each empty message received.
        /// </summary>
        /// <remarks>
        /// Waits a maximum of 20 milliseconds.
        /// </remarks>
        LowLatency
    }
}