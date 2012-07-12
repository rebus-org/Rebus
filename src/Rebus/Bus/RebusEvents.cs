using System;

namespace Rebus.Bus
{
    internal class RebusEvents : IRebusEvents
    {
        /// <summary>
        /// Event that will be raised immediately when the bus is used to send a logical message.
        /// </summary>
        public event Action<string, object> MessageSent = delegate { };

        /// <summary>
        /// Event that will be raised for each received logical message (i.e. it will only be called
        /// if deserialization completed, and the transport message does in fact contain one or more
        /// logical messages).
        /// </summary>
        public event Action<object> MessageReceived = delegate { };

        /// <summary>
        /// Event that will be raised immediately after receiving a transport 
        /// message, before any other actions are executed.
        /// </summary>
        public event Action<ReceivedTransportMessage> BeforeTransportMessage = delegate { };

        /// <summary>
        /// Event that will be raised after a transport message has been handled.
        /// If an error occurs, the caught exception will be passed to the
        /// listeners. If no errors occur, the passed exception will be null.
        /// </summary>
        public event Action<Exception, ReceivedTransportMessage> AfterTransportMessage = delegate { };

        /// <summary>
        /// Event that will be raised whenever it is determined that a message
        /// has failed too many times.
        /// </summary>
        public event Action<ReceivedTransportMessage> PoisonMessage = delegate { };

        internal void RaiseMessageSent(string destination, object message)
        {
            MessageSent(destination, message);
        }

        internal void RaiseMessageReceived(object message)
        {
            MessageReceived(message);
        }

        internal void RaiseBeforeTransportMessage(ReceivedTransportMessage transportMessage)
        {
            BeforeTransportMessage(transportMessage);
        }

        internal void RaiseAfterTransportMessage(Exception exception, ReceivedTransportMessage transportMessage)
        {
            AfterTransportMessage(exception, transportMessage);
        }

        internal void RaisePoisonMessage(ReceivedTransportMessage transportMessage)
        {
            PoisonMessage(transportMessage);
        }
    }
}