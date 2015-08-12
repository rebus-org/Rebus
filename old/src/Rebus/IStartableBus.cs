using Rebus.Bus;

namespace Rebus
{
    /// <summary>
    /// Represents a bus that wants to be started before it can be used.
    /// </summary>
    public interface IStartableBus
    {
        /// <summary>
        /// Starts the bus.
        /// </summary>
        IBus Start();

        /// <summary>
        /// Starts the <see cref="RebusBus"/> with the specified number of worker threads.
        /// </summary>
        IBus Start(int numberOfWorkers);
    }
}