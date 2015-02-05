using System.Collections.Generic;

namespace Rebus2.Messages
{
    public class Message
    {
        public Message(Dictionary<string, string> headers, object body)
        {
            Headers = headers;
            Body = body;
        }

        public Dictionary<string, string> Headers { get; private set; }

        public object Body { get; private set; }
    }
}