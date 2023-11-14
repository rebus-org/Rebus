using System.Threading.Tasks;

namespace Rebus.Transport;

/// <summary>
/// Interface of a <see cref="ITransactionContext"/> that can "eager commit", meaning that it'll carry out its internal commit behavior when told to do so.
/// This finer level of control can be used in situations where the caller wants to ensure that the commit actions are carried out in a specific place.
/// </summary>
public interface ICanEagerCommit : ITransactionContext
{
    /// <summary>
    /// Commits the transaction context
    /// </summary>
    Task CommitAsync();
}