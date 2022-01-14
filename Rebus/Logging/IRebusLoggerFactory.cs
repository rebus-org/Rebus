namespace Rebus.Logging;

/// <summary>
/// Basic interface of a Rebus logger factory. You can make a tiny shortcut by deriving from <see cref="AbstractRebusLoggerFactory"/> if you intend to implement this interface
/// </summary>
public interface IRebusLoggerFactory
{
    /// <summary>
    /// Gets a logger for the type <typeparamref name="T"/>
    /// </summary>
    ILog GetLogger<T>();
}