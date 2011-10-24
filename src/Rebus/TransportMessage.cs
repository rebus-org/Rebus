namespace Rebus
{
    /// <summary>
    /// Low-level transport message object.
    /// </summary>
    public class TransportMessage
    {
        /// <summary>
        /// Id given to this message, most likely by the queue infrastructure.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Data of whatever header and body information this message may contain.
        /// </summary>
        public byte[] Data { get; set; }
    }
}