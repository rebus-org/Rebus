using System;

namespace Rebus
{
    /// <summary>
    /// Delegate type that can listen to whenever the bus sends a logical message.
    /// </summary>
    public delegate void MessageSentEventHandler(IAdvancedBus advancedBus, string destination, object message);
    
    /// <summary>
    /// Delegate type that can listen to whenever the bus received a logical message.
    /// </summary>
    public delegate void MessageReceivedEventHandler(IAdvancedBus advancedBus, object message);
    
    /// <summary>
    /// Delegate type that can listen to whenever the bus has received a transport message, but it has not yet been deserialized.
    /// </summary>
    public delegate void BeforeTransportMessageEventHandler(IAdvancedBus advancedBus, ReceivedTransportMessage receivedTransportMessage);
    
    /// <summary>
    /// Delegate type that can listen to whenever the bus has received and dispatched a transport message, and then - depending on how that
    /// went - and exception might be passed along.
    /// </summary>
    public delegate void AfterTransportMessageEventHandler(IAdvancedBus advancedBus, Exception exceptionOrNull, ReceivedTransportMessage receivedTransportMessage);
    
    /// <summary>
    /// Delegate type that can listen to whenever the bus has decided that message is poison, and should be moved to the error queue.
    /// </summary>
    public delegate void PoisonMessageEventHandler(IAdvancedBus advancedBus, ReceivedTransportMessage receivedTransportMessage);

    /// <summary>
    /// Groups the different event hooks that Rebus exposes.
    /// </summary>
    public interface IRebusEvents
    {
        /// <summary>
        /// Event that will be raised immediately after receiving a transport 
        /// message, before any other actions are executed.
        /// </summary>
        event BeforeTransportMessageEventHandler BeforeTransportMessage;

        /// <summary>
        /// Event that will be raised after a transport message has been handled.
        /// If an error occurs, the caught exception will be passed to the
        /// listeners. If no errors occur, the passed exception will be null.
        /// </summary>
        event AfterTransportMessageEventHandler AfterTransportMessage;

        /// <summary>
        /// Event that will be raised whenever it is determined that a message
        /// has failed too many times.
        /// </summary>
        event PoisonMessageEventHandler PoisonMessage;

        /// <summary>
        /// Event that will be raised immediately when the bus is used to send a logical message.
        /// </summary>
        event MessageSentEventHandler MessageSent;

        /// <summary>
        /// Event that will be raised for each received logical message (i.e. it will only be called
        /// if deserialization completed, and the transport message does in fact contain one or more
        /// logical messages).
        /// </summary>
        event MessageReceivedEventHandler MessageReceived;
    }
}