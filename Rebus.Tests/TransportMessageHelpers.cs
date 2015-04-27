using System.Collections.Generic;
using System.Text;
using Rebus.Messages;

namespace Rebus.Tests
{
    public static class TransportMessageHelpers
    {
        public static TransportMessage FromString(string body)
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            var headers = new Dictionary<string, string>();
            return new TransportMessage(headers, bytes);
        }
    }
}