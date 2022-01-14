using System.Threading;
using System.Threading.Tasks;
using Rebus.Messages;

namespace Rebus.Transport;

/// <summary>
/// The transport is responsible for sending and receiving messages
/// </summary>
public interface ITransport
{
    /// <summary>
    /// Must create a queue with the given address
    /// </summary>
    void CreateQueue(string address);

    /// <summary>
    /// Sends the given <see cref="TransportMessage"/> to the queue with the specified globally addressable name
    /// </summary>
    Task Send(string destinationAddress, TransportMessage message, ITransactionContext context);

    /// <summary>
    /// Receives the next message (if any) from the transport's input queue <see cref="Address"/>
    /// </summary>
    Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken);
        
    /// <summary>
    /// Gets the global address of the transport's input queue
    /// </summary>
    string Address { get; }
}