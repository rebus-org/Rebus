using System;

namespace Rebus
{
    /// <summary>
    /// Groups the different event hooks that Rebus exposes.
    /// </summary>
    public interface IRebusEvents
    {
        /// <summary>
        /// Event that will be raised immediately after receiving a transport 
        /// message, before any other actions are executed.
        /// </summary>
        event Action<ReceivedTransportMessage> BeforeTransportMessage;

        /// <summary>
        /// Event that will be raised after a transport message has been handled.
        /// If an error occurs, the caught exception will be passed to the
        /// listeners. If no errors occur, the passed exception will be null.
        /// </summary>
        event Action<Exception, ReceivedTransportMessage> AfterTransportMessage;

        /// <summary>
        /// Event that will be raised whenever it is determined that a message
        /// has failed too many times.
        /// </summary>
        event Action<ReceivedTransportMessage> PoisonMessage;

        /// <summary>
        /// Event that will be raised immediately when the bus is used to send a logical message.
        /// </summary>
        event Action<string, object> MessageSent;

        /// <summary>
        /// Event that will be raised for each received logical message (i.e. it will only be called
        /// if deserialization completed, and the transport message does in fact contain one or more
        /// logical messages).
        /// </summary>
        event Action<object> MessageReceived;
    }
}