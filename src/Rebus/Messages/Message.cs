using System.Collections.Generic;
using System.Linq;

namespace Rebus.Messages
{
    /// <summary>
    /// Message wrapper object that may contain multiple logical messages.
    /// </summary>
    public class Message
    {
        public Message()
        {
            Headers = new Dictionary<string, string>();
        }

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
                return "Empty Message";

            return string.Join(" + ", Messages.Select(m => m.GetType().Name));
        }
    }

    public class Headers
    {
        public const string ReturnAddress = "returnAddress";
        public const string ErrorMessage = "errorMessage";
    }
}