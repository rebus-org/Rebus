using System;
using System.Collections.Generic;

namespace Rebus.Messages
{
    /// <summary>
    /// Logical message wrapper that has a set of headers and a .NET object
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Constructs the message with the specified headers, wrapping the given .NET object as the message body
        /// </summary>
        public Message(Dictionary<string, string> headers, object body)
        {
            Headers = headers ?? throw new ArgumentNullException(nameof(headers));
            Body = body ?? throw new ArgumentNullException(nameof(body));
        }

        /// <summary>
        /// Gets the headers of this message
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        /// Gets the wrapped body object of this message
        /// </summary>
        public object Body { get; }
    }
}