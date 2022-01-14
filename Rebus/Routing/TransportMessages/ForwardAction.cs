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
        DestinationAddresses = destinationAddresses.ToList();
        ActionType = actionType;
    }

    /// <summary>
    /// Gets an action that causes the message to be handled normally
    /// </summary>
    public static ForwardAction None = new ForwardAction(ActionType.None);

    /// <summary>
    /// Gets an action that causes the message to be forwarded to the queue specified by <paramref name="destinationAddress"/>
    /// </summary>
    public static ForwardAction ForwardTo(string destinationAddress)
    {
        if (destinationAddress == null) throw new ArgumentNullException(nameof(destinationAddress), "Cannot forward message to (NULL) - use ForwardAction.None if you don't intend to forward the message");

        return new ForwardAction(ActionType.Forward, destinationAddress);
    }

    /// <summary>
    /// Gets an action that causes the message to be forwarded to the queues specified by <paramref name="destinationAddresses"/>
    /// </summary>
    public static ForwardAction ForwardTo(IEnumerable<string> destinationAddresses)
    {
        if (destinationAddresses == null) throw new ArgumentNullException(nameof(destinationAddresses), "Cannot forward message to (NULL) - use ForwardAction.None if you don't intend to forward the message");

        return new ForwardAction(ActionType.Forward, destinationAddresses.ToArray());
    }

    /// <summary>
    /// Gets an action that causes the messge to be ignored. THIS WILL EFFECTIVELY LOSE THE MESSAGE
    /// </summary>
    public static ForwardAction Ignore()
    {
        return new ForwardAction(ActionType.Ignore);
    }

    internal List<string> DestinationAddresses { get; }

    internal ActionType ActionType { get; }
}