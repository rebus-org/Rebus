using System;

namespace Rebus
{
    /// <summary>
    /// Extends the capabilities of <see cref="IBus"/> with some more advanced features.
    /// </summary>
    public interface IAdvancedBus : IBus
    {
        /// <summary>
        /// Event that will be raised immediately after receiving a transport 
        /// message, before any other actions are executed.
        /// </summary>
        event Action<ReceivedTransportMessage> BeforeMessage;

        /// <summary>
        /// Event that will be raised after a transport message has been handled.
        /// If an error occurs, the caught exception will be passed to the
        /// listeners. If no errors occur, the passed exception will be null.
        /// </summary>
        event Action<Exception, ReceivedTransportMessage> AfterMessage;

        /// <summary>
        /// Event that will be raised whenever it is determined that a message
        /// has failed too many times.
        /// </summary>
        event Action<ReceivedTransportMessage> PoisonMessage;

        /// <summary>
        /// Sends the specified batch of messages, dividing the batch into bacthes
        /// for individual recipients if necessary. For each recipient, the order
        /// of the messages within the batch is preserved.
        /// </summary>
        void SendBatch(params object[] messages);

        /// <summary>
        /// Publishes the specified batch of messages, dividing the batch into
        /// batches for individual recipients if necessary. For each subscriber,
        /// the order of the messages within the batch is preserved.
        /// </summary>
        void PublishBatch(params object[] messages);
    }
}