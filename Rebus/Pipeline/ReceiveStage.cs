namespace Rebus.Pipeline
{
    /// <summary>
    /// Indicates some known stages within a receive pipeline, allowing for <see cref="IIncomingStep"/> implementations to position themselves correctly without necessarily
    /// knowing anything about any other steps
    /// </summary>
    public enum ReceiveStage
    {
        /// <summary>
        /// This stage is executed first thing after the message has been received
        /// </summary>
        TransportMessageReceived = 1000,
        
        /// <summary>
        /// This stage is executed once the message body has been properly decoded and deserialized and most likely turned into a .NET object
        /// </summary>
        MessageDeserialized = 2000,
    }
}