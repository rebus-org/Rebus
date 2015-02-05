using System.Collections.Generic;
using System.IO;
using System.Text;
using Rebus2.Messages;
using Rebus2.Msmq;

namespace Tests
{
    public static class TransportMessageHelpers
    {
        public static TransportMessage FromString(string body)
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            var memoryStream = new MemoryStream(bytes);
            var headers = new Dictionary<string, string>();
            return new TransportMessage(headers, memoryStream);
        }

    }
}