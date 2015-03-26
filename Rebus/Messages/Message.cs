using System;
using System.Collections.Generic;

namespace Rebus.Messages
{
    /// <summary>
    /// Logical message wrapper that has a set of headers and a .NET object
    /// </summary>
    public class Message
    {
        public Message(Dictionary<string, string> headers, object body)
        {
            if (headers == null) throw new ArgumentNullException("headers");
            if (body == null) throw new ArgumentNullException("body");
            Headers = headers;
            Body = body;
        }

        public Dictionary<string, string> Headers { get; private set; }

        public object Body { get; private set; }
    }
}