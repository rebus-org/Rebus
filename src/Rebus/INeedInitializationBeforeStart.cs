using Rebus.Bus;

namespace Rebus
{
    /// <summary>
    /// Services injected into <see cref="RebusBus"/> may implement this in order to be initialized right before the bus is started
    /// </summary>
    public interface INeedInitializationBeforeStart
    {
        /// <summary>
        /// Allows the implementor to perform some kind of initialization
        /// </summary>
        void Initialize();
    }
}