namespace Rebus.Bus;

/// <summary>
/// Anything that is resolved with the injectionist can be marked as initializable by implementing this interface, which
/// will then have its <see cref="Initialize"/> method called before the bus is started
/// </summary>
public interface IInitializable
{
    /// <summary>
    /// Initializes the instance
    /// </summary>
    void Initialize();
}