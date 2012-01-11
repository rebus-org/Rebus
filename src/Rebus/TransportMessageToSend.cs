using System.Collections.Generic;

namespace Rebus
{
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

        /// <summary>
        /// String label to use if the underlying message queue supports it
        /// </summary>
        public string Label { get; set; }
    }
}