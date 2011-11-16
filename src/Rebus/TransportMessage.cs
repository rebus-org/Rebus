using System.Collections.Generic;

namespace Rebus
{
    ///// <summary>
    ///// Low-level transport message object.
    ///// </summary>
    //public class TransportMessage
    //{
    //    /// <summary>
    //    /// Id given to this message, most likely by the queue infrastructure.
    //    /// </summary>
    //    public string Id { get; set; }

    //    /// <summary>
    //    /// Copy of the message headers
    //    /// </summary>
    //    public Dictionary<string, string> Headers { get; set; }

    //    /// <summary>
    //    /// Data of whatever header and body information this message may contain.
    //    /// </summary>
    //    public byte[] Data { get; set; }
    //}

    public class TransportMessageToSend
    {
        /// <summary>
        /// Copy of the message headers
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Data of whatever header and body information this message may contain.
        /// </summary>
        public string Data { get; set; }
    }

    public class ReceivedTransportMessage
    {
        /// <summary>
        /// Id given to this message, most likely by the queue infrastructure.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Data of whatever header and body information this message may contain.
        /// </summary>
        public string Data { get; set; }
    }
}