using System.Threading.Tasks;

namespace Rebus.Workers.ThreadPoolBased
{
    /// <summary>
    /// Implements a strategy with which workers will back off in idle periods. Please note that the <see cref="IAsyncBackoffStrategy"/>
    /// implementations must be reentrant!
    /// </summary>
    public interface IAsyncBackoffStrategy
    {
        /// <summary>
        /// Executes the next wait operation by blocking the thread, possibly advancing the wait cursor to a different wait time for the next time.
        /// This function is called each time a worker thread cannot continue because no more parallel operations are allowed to happen.
        /// </summary>
        void Wait();

        /// <summary>
        /// Executes the next wait operation by blocking the thread, possibly advancing the wait cursor to a different wait time for the next time.
        /// This function is called each time no message was received.
        /// </summary>
        Task WaitNoMessage();

        /// <summary>
        /// Blocks the thread for a (most likely longer) while, when an error has occurred
        /// </summary>
        Task WaitError();

	    /// <summary>
	    /// Resets the strategy. Is called whenever a message was received.
	    /// </summary>
	    Task Reset();
    }
}