using System;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Routing.TransportMessages;

/// <summary>
/// Represents some action to perform with the incoming transport message. Must be created via the static functions
/// </summary>
public class ForwardAction
{
    ForwardAction(ActionType actionType, params string[] destinationAddresses)
    {
        if (destinationAddresses == null) throw new ArgumentNullException(nameof(destinationAddresses));
        DestinationQueueNames = destinationAddresses.ToList();
        ActionType = actionType;
    }

    /// <summary>
    /// Gets an action that causes the message to be handled normally
    /// </summary>
    public static ForwardAction None = new(ActionType.None);

    /// <summary>
    /// Gets an action that causes the message to be forwarded to the queue specified by <paramref name="destinationQueueName"/>
    /// </summary>
    public static ForwardAction ForwardTo(string destinationQueueName)
    {
        if (destinationQueueName == null) throw new ArgumentNullException(nameof(destinationQueueName), "Cannot forward message to (NULL) - use ForwardAction.None if you don't intend to forward the message");

        return new ForwardAction(ActionType.Forward, destinationQueueName);
    }

    /// <summary>
    /// Gets an action that causes the message to be forwarded to the queues specified by <paramref name="destinationQueueNames"/>
    /// </summary>
    public static ForwardAction ForwardTo(IEnumerable<string> destinationQueueNames)
    {
        if (destinationQueueNames == null) throw new ArgumentNullException(nameof(destinationQueueNames), "Cannot forward message to (NULL) - use ForwardAction.None if you don't intend to forward the message");

        return new ForwardAction(ActionType.Forward, destinationQueueNames.ToArray());
    }

    /// <summary>
    /// Gets an action that causes the messge to be ignored. THIS WILL EFFECTIVELY LOSE THE MESSAGE
    /// </summary>
    public static ForwardAction Ignore() => new(ActionType.Ignore);

    internal List<string> DestinationQueueNames { get; }

    internal ActionType ActionType { get; }
}