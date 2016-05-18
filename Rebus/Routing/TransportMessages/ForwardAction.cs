using System;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Routing.TransportMessages
{
    /// <summary>
    /// Represents some action to perform with the incoming transport message. Must be created via the static functions
    /// </summary>
    public class ForwardAction
    {
        readonly List<string> _destinationAddresses;
        readonly ActionType _actionType;

        ForwardAction(ActionType actionType, params string[] destinationAddresses)
        {
            _destinationAddresses = destinationAddresses.ToList();
            _actionType = actionType;
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

        internal List<string> DestinationAddresses
        {
            get { return _destinationAddresses; }
        }

        internal ActionType ActionType
        {
            get { return _actionType; }
        }
    }
}