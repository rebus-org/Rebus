using System;
using System.Collections.Generic;

namespace Rebus.Messages
{
    /// <summary>
    /// Transport message wrapper that has a set of headers and a stream of raw data to be sent/received
    /// </summary>
    public class TransportMessage
    {
        /// <summary>
        /// Constructs the transport message with the given headers, wrapping the given body payload
        /// </summary>
        public TransportMessage(Dictionary<string, string> headers, byte[] body)
        {
            if (headers == null) throw new ArgumentNullException(nameof(headers));
            if (body == null) throw new ArgumentNullException(nameof(body));
            Headers = headers;
            Body = body;
        }

        /// <summary>
        /// Gets the headers of this message
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }

        /// <summary>
        /// Gets the wrapped body data of this message
        /// </summary>
        public byte[] Body { get; private set; }
    }
}