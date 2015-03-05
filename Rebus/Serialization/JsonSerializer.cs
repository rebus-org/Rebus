using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rebus.Extensions;
using Rebus.Messages;

namespace Rebus.Serialization
{
    public class JsonSerializer : ISerializer
    {
        public static string JsonUtf8ContentType = "application/json;charset=utf-8";

        static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        static readonly Encoding DefaultEncoding = Encoding.UTF8;

        public async Task<TransportMessage> Serialize(Message message)
        {
            var jsonText = JsonConvert.SerializeObject(message.Body, Settings);
            var stream = new MemoryStream(DefaultEncoding.GetBytes(jsonText));
            var headers = message.Headers.Clone();
            headers[Headers.ContentType] = JsonUtf8ContentType;
            return new TransportMessage(headers, stream);
        }

        public async Task<Message> Deserialize(TransportMessage transportMessage)
        {
            var contentType = transportMessage.Headers.GetValue(Headers.ContentType);

            if (contentType != JsonUtf8ContentType)
            {
                throw new FormatException(string.Format("Unknown content type: '{0}' - must be '{1}' for the JSON serialier to work", contentType, JsonUtf8ContentType));
            }

            using (var reader = new StreamReader(transportMessage.Body, DefaultEncoding))
            {
                var bodyString = await reader.ReadToEndAsync();
                var bodyObject = JsonConvert.DeserializeObject(bodyString, Settings);
                var headers = transportMessage.Headers.Clone();
                return new Message(headers, bodyObject);
            }
        }
    }
}