using System.Threading.Tasks;

namespace Rebus.Workers
{
    /// <summary>
    /// Implements a strategy with which workers will back off in idle periods. Please note that the <see cref="IBackoffStrategy"/>
    /// implementations must be reentrant!
    /// </summary>
    public interface IBackoffStrategy
    {
        /// <summary>
        /// Asynchronously executes the next wait operation, possibly advancing the wait cursor to a different wait time for the next time.
        /// This function is called each time no message was received.
        /// </summary>
        Task Wait();

        /// <summary>
        /// Resets the strategy. Is called whenever a message was received.
        /// </summary>
        void Reset();
    }
}