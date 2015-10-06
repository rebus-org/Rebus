using System.Diagnostics;

namespace Rebus.Logging
{
    /// <summary>
    /// Basic interface of a Rebus logger factory. If you intend to implement your own,
    /// <see cref="AbstractRebusLoggerFactory"/> is the one to derive from - you should
    /// probably not implement this interface directly.
    /// </summary>
    public interface IRebusLoggerFactory
    {
        /// <summary>
        /// Gets a logger that is initialized to somehow carry information on the class that is using it.
        /// Be warned that this method will most likely be pretty slow, because it will probably rely on
        /// some clunky <see cref="StackFrame"/> inspection.
        /// </summary>
        ILog GetCurrentClassLogger();

        /// <summary>
        /// Gets a logger for the type <typeparamref name="T"/>
        /// </summary>
        ILog GetLogger<T>();
    }
}