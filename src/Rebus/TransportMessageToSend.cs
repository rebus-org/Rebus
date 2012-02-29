using System.Collections.Generic;

namespace Rebus
{
    public class TransportMessageToSend
    {
        public TransportMessageToSend()
        {
            Headers = new Dictionary<string, string>();
        }

        /// <summary>
        /// Message headers. Pre-defined header keys can be found in <see cref="Messages.Headers"/>.
        /// </summary>
        public IDictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Message body. Should not contain any header information.
        /// </summary>
        public byte[] Body { get; set; }

        /// <summary>
        /// String label to use if the underlying message queue supports it.
        /// </summary>
        public string Label { get; set; }
    }
}