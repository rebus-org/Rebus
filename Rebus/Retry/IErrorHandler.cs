using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Retry;

/// <summary>
/// Serivce that gets to handle failed messages
/// </summary>
public interface IErrorHandler
{
    /// <summary>
    /// Handles the poisonous message in the right way
    /// </summary>
    Task HandlePoisonMessage(TransportMessage transportMessage, ITransactionContext transactionContext, ExceptionInfo exception);
}