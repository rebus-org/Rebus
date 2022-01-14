using System.Collections.Generic;
using System.Linq;

namespace Rebus.Sagas.Idempotent;

/// <summary>
/// Contains all the <see cref="OutgoingMessage"/> instances for a given incoming message
/// </summary>
public class OutgoingMessages
{
    readonly List<OutgoingMessage> _messagesToSend;

    /// <summary>
    /// Constructs the instance for the given message ID, containing the given <see cref="OutgoingMessage"/> instances
    /// </summary>
    public OutgoingMessages(string messageId, IEnumerable<OutgoingMessage> messagesToSend)
    {
        MessageId = messageId;
        _messagesToSend = (messagesToSend ?? Enumerable.Empty<OutgoingMessage>()).ToList();
    }

    /// <summary>
    /// Gets the ID of the incoming message
    /// </summary>
    public string MessageId { get; }

    /// <summary>
    /// Gets all the outgoing messages to be sent as a consequence of handling the message with the ID <see cref="MessageId"/>
    /// </summary>
    public IEnumerable<OutgoingMessage> MessagesToSend => _messagesToSend;

    /// <summary>
    /// Adds another <see cref="OutgoingMessage"/> as a side-effect of handling the message with the ID <see cref="MessageId"/>
    /// </summary>
    public void Add(OutgoingMessage outgoingMessage) => _messagesToSend.Add(outgoingMessage);
}