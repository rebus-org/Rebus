using System;

namespace Rebus.Bus
{
    class RebusEvents : IRebusEvents
    {
        /// <summary>
        /// Event that will be raised immediately when the bus is used to send a logical message.
        /// </summary>
        public event MessageSentEventHandler MessageSent = delegate { };

        /// <summary>
        /// Event that will be raised for each received logical message (i.e. it will only be called
        /// if deserialization completed, and the transport message does in fact contain one or more
        /// logical messages).
        /// </summary>
        public event BeforeMessageEventHandler BeforeMessage = delegate { };

        /// <summary>
        /// Event that will be raised for each received logical message (i.e. it will only be called
        /// if deserialization completed, and the transport message does in fact contain one or more
        /// logical messages).
        /// </summary>
        public event AfterMessageEventHandler AfterMessage = delegate { };

        /// <summary>
        /// Event that will be raised immediately after receiving a transport 
        /// message, before any other actions are executed.
        /// </summary>
        public event BeforeTransportMessageEventHandler BeforeTransportMessage = delegate { };

        /// <summary>
        /// Event that will be raised after a transport message has been handled.
        /// If an error occurs, the caught exception will be passed to the
        /// listeners. If no errors occur, the passed exception will be null.
        /// </summary>
        public event AfterTransportMessageEventHandler AfterTransportMessage = delegate { };

        /// <summary>
        /// Event that will be raised whenever it is determined that a message
        /// has failed too many times.
        /// </summary>
        public event PoisonMessageEventHandler PoisonMessage = delegate { };

        internal void RaiseMessageSent(IAdvancedBus advancedBus, string destination, object message)
        {
            MessageSent(advancedBus, destination, message);
        }

        internal void RaiseBeforeMessage(IAdvancedBus advancedBus, object message)
        {
            BeforeMessage(advancedBus, message);
        }

        internal void RaiseAfterMessage(IAdvancedBus bus, Exception exception, object message)
        {
            AfterMessage(bus, exception, message);
        }

        internal void RaiseBeforeTransportMessage(IAdvancedBus advancedBus, ReceivedTransportMessage transportMessage)
        {
            BeforeTransportMessage(advancedBus, transportMessage);
        }

        internal void RaiseAfterTransportMessage(IAdvancedBus advancedBus, Exception exception, ReceivedTransportMessage transportMessage)
        {
            AfterTransportMessage(advancedBus, exception, transportMessage);
        }

        internal void RaisePoisonMessage(IAdvancedBus advancedBus, ReceivedTransportMessage transportMessage)
        {
            PoisonMessage(advancedBus, transportMessage);
        }
    }
}