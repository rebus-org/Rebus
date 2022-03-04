using System.Collections.Generic;
using System.Linq;
using Rebus.Messages;

namespace Rebus.Sagas.Idempotent;

/// <summary>
/// This chunk of data help with tracking handled messages and externally visible behavior (i.e. outbound messages) from handling each message
/// </summary>
public class IdempotencyData
{
    /// <summary>
    /// Creates the idempotency data object, initializing it with the given list of outgoing message lists and record of handled message IDs
    /// </summary>
    public IdempotencyData(IEnumerable<OutgoingMessages> outgoingMessages = null, IEnumerable<string> handledMessageIds = null)
    {
        if (outgoingMessages != null)
        {
            OutgoingMessages.AddRange(outgoingMessages);
        }

        if (handledMessageIds != null)
        {
            foreach (var id in handledMessageIds)
            {
                HandledMessageIds.Add(id);
            }
        }
    }

    /// <summary>
    /// Gets the outgoing messages
    /// </summary>
    public List<OutgoingMessages> OutgoingMessages { get; } = new List<OutgoingMessages>();

    /// <summary>
    /// Getst the IDs of all messages that have been handled
    /// </summary>
    public HashSet<string> HandledMessageIds { get; } = new HashSet<string>();

    /// <summary>
    /// Gets whether the message with the given ID has already been handled
    /// </summary>
    public bool HasAlreadyHandled(string messageId) => HandledMessageIds.Contains(messageId);

    /// <summary>
    /// Gets the outgoing messages for the incoming message with the given ID
    /// </summary>
    public IEnumerable<OutgoingMessage> GetOutgoingMessages(string messageId)
    {
        var outgoingMessages = OutgoingMessages.FirstOrDefault(o => o.MessageId == messageId);

        return outgoingMessages != null
            ? outgoingMessages.MessagesToSend
            : Enumerable.Empty<OutgoingMessage>();
    }

    /// <summary>
    /// Marks the message with the given ID as handled
    /// </summary>
    public void MarkMessageAsHandled(string messageId) => HandledMessageIds.Add(messageId);

    /// <summary>
    /// Adds the <see cref="TransportMessage"/> as an outgoing message destined for the addresses specified by <paramref name="destinationAddresses"/>
    /// under the given <paramref name="messageId"/>
    /// </summary>
    public void AddOutgoingMessage(string messageId, IEnumerable<string> destinationAddresses, TransportMessage transportMessage)
    {
        var outgoingMessage = new OutgoingMessage(destinationAddresses, transportMessage);

        GetOrCreate(messageId).Add(outgoingMessage);
    }

    OutgoingMessages GetOrCreate(string messageId)
    {
        HandledMessageIds.Add(messageId);

        var outgoingMessages = OutgoingMessages.FirstOrDefault(o => o.MessageId == messageId);

        if (outgoingMessages != null) return outgoingMessages;

        outgoingMessages = new OutgoingMessages(messageId, new List<OutgoingMessage>());
        OutgoingMessages.Add(outgoingMessages);

        return outgoingMessages;
    }
}