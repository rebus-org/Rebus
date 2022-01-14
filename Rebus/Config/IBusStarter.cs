using Rebus.Bus;

namespace Rebus.Config;

/// <summary>
/// Wraps a bus, which has had its message processing stopped, by setting number of workers to 0.
/// When <see cref="Start"/> is called, workers are added, and message processing will start.
/// </summary>
public interface IBusStarter
{
    /// <summary>
    /// Starts message processing and returns the bus instance
    /// </summary>
    IBus Start();

    /// <summary>
    /// Gets the bus instance wrapped in this starter. The bus can be used to send, publish, subscribe, etc.
    /// </summary>
    IBus Bus { get; }
}