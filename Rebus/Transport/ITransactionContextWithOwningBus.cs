using Rebus.Bus;

namespace Rebus.Transport;

/// <summary>
/// Extends <see cref="ITransactionContext"/> with an "owning bus", which makes it possible for a bus instance to avoid enlisting
/// its work in an ongoing transaction if it detects that it's another bus' context
/// </summary>
public interface ITransactionContextWithOwningBus : ITransactionContext
{
    /// <summary>
    /// Gets the owning bus instance
    /// </summary>
    IBus OwningBus { get; }
}