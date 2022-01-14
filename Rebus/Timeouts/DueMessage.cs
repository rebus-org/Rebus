using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Messages;

namespace Rebus.Timeouts;

/// <summary>
/// Represents a message that was deferred and is now due. The message has some headers and a body and can be turned into
/// a <see cref="TransportMessage"/> by calling <see cref="ToTransportMessage"/>. The due message can be constructed in a
/// way that can perform an arbitrary action in order to mark the due message as successfully delivered.
/// </summary>
public class DueMessage
{
    readonly Func<Task> _completeAction;

    /// <summary>
    /// Constructs the due message with the given headers and body, storing the given <paramref name="completeAction"/> to be
    /// executed when the message's <see cref="MarkAsCompleted"/> method is called.
    /// </summary>
    public DueMessage(Dictionary<string, string> headers, byte[] body, Func<Task> completeAction = null)
    {
        _completeAction = completeAction;
        Headers = headers ?? throw new ArgumentNullException(nameof(headers));
        Body = body ?? throw new ArgumentNullException(nameof(body));
    }

    /// <summary>
    /// Gets the headers of this due message
    /// </summary>
    public Dictionary<string, string> Headers { get; }

    /// <summary>
    /// Gets the body data of this due message
    /// </summary>
    public byte[] Body { get; }

    /// <summary>
    /// Marks the due message as successfully handled, which should probably be done when the message has been safely sent to the proper recipient
    /// </summary>
    public async Task MarkAsCompleted()
    {
        if (_completeAction == null) return;

        await _completeAction();
    }

    /// <summary>
    /// Returns the headers and the body of this due message in a <see cref="TransportMessage"/>
    /// </summary>
    public TransportMessage ToTransportMessage() => new TransportMessage(Headers, Body);
}