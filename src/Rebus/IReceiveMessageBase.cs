namespace Rebus
{
    /// <summary>
    /// Base interface for <see cref="IReceiveMessages"/> and <see cref="IReceiveMessagesAsync"/> for something capable of
    /// receiving messages synchronously and asynchronously, correspondently.
    /// </summary>
    public interface IReceiveMessageBase
    {
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