using System.Collections.Generic;
using System.IO;

namespace Rebus2.Messages
{
    public class TransportMessage
    {
        public TransportMessage(Dictionary<string, string> headers, Stream body)
        {
            Headers = headers;
            Body = body;
        }

        public Dictionary<string, string> Headers { get; private set; }
        
        public Stream Body { get; private set; }
    }
}