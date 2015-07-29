using System;
using System.Collections.Generic;

namespace Rebus
{
    /// <summary>
    /// Message container that contains the parts of a single transport message that should be sent.
    /// It carries a headers dictionary and a body byte array. The <seealso cref="Label"/> can be used
    /// to label the message somehow, which can then be used to show the message if the infrastructure
    /// supports it.
    /// </summary>
    [Serializable]
    public class TransportMessageToSend
    {
        /// <summary>
        /// Constructs the wrapper of a transport message that is about to be sent
        /// </summary>
        public TransportMessageToSend()
        {
            Headers = new Dictionary<string, object>();
        }

        /// <summary>
        /// Message headers. Pre-defined header keys can be found in <see cref="Shared.Headers"/>.
        /// </summary>
        public IDictionary<string, object> Headers { get; set; }

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