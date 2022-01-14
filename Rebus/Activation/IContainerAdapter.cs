using Rebus.Bus;

namespace Rebus.Activation;

/// <summary>
/// Extends <see cref="IHandlerActivator"/> with the expectation that it is backed by some kind of IoC container that can hold
/// a bus instance (which it naturally should be able to inject into handlers when they're activated)
/// </summary>
public interface IContainerAdapter : IHandlerActivator
{
    /// <summary>
    /// Sets the bus instance that this <see cref="IContainerAdapter"/> should be able to inject when resolving handler instances
    /// </summary>
    void SetBus(IBus bus);
}