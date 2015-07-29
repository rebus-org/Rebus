namespace Rebus
{
    /// <summary>
    /// Interface of something that is capable of receiving messages. If no message is available,
    /// null should be returned. If the bus is configured to run with multiple threads, this one
    /// should be reentrant.
    /// </summary>
    public interface IReceiveMessages
    {
        /// <summary>
        /// Attempt to receive the next available message. Should return null if no message is available.
        /// </summary>
        ReceivedTransportMessage ReceiveMessage(ITransactionContext context);
        
        /// <summary>
        /// Gets the name of this receiver's input queue - i.e. this is the queue that this receiver
        /// will pull messages from.
        /// </summary>
        string InputQueue { get; }

        /// <summary>
        /// Gets the globally accessible adddress of this receiver's input queue - i.e. this would probably
        /// be the input queue in some form, possible qualified by machine name or something similar.
        /// </summary>
        string InputQueueAddress { get; }
    }
}