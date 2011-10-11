using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Messages
{
    /// <summary>
    /// Message wrapper object that may contain multiple logical messages.
    /// </summary>
    public class TransportMessage
    {
        public TransportMessage()
        {
            Headers = new Dictionary<string, string>();
        }

        public string Id { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public object[] Messages { get; set; }
        
        public string GetHeader(string key)
        {
            return Headers.ContainsKey(key)
                       ? Headers[key]
                       : null;
        }

        public string GetLabel()
        {
            if (Messages == null || Messages.Length == 0)
                return "Empty TransportMessage";

            return Messages.Length == 1
                       ? Messages[0].GetType().Name
                       : string.Join(", ", Messages.Select(m => m.GetType().Name));
        }
    }
}