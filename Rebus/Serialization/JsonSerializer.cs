using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rebus.Extensions;
using Rebus.Messages;
#pragma warning disable 1998

namespace Rebus.Serialization
{
    /// <summary>
    /// Implementation of <see cref="ISerializer"/> that uses Newtonsoft JSON.NET internally, with some pretty robust settings
    /// (i.e. full type info is included in the serialized format in order to support deserializing "unknown" types like
    /// implementations of interfaces, etc)
    /// </summary>
    class JsonSerializer : ISerializer
    {
        /// <summary>
        /// Proper content type when a message has been serialized with this serializer (or another compatible JSON serializer) and it uses the standard UTF8 encoding
        /// </summary>
        public const string JsonUtf8ContentType = "application/json;charset=utf-8";

        /// <summary>
        /// Contents type when the content is JSON
        /// </summary>
        public const string JsonContentType = "application/json";

        static readonly JsonSerializerSettings DefaultSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        static readonly Encoding DefaultEncoding = Encoding.UTF8;

        readonly JsonSerializerSettings _settings;
        readonly Encoding _encoding;

        public JsonSerializer()
            : this(DefaultSettings, DefaultEncoding)
        {
        }

        internal JsonSerializer(Encoding encoding)
            : this(DefaultSettings, encoding)
        {
        }

        internal JsonSerializer(JsonSerializerSettings jsonSerializerSettings)
            : this(jsonSerializerSettings, DefaultEncoding)
        {
        }

        internal JsonSerializer(JsonSerializerSettings jsonSerializerSettings, Encoding encoding)
        {
            _settings = jsonSerializerSettings;
            _encoding = encoding;
        }

        /// <summary>
        /// Serializes the given <see cref="Message"/> into a <see cref="TransportMessage"/>
        /// </summary>
        public async Task<TransportMessage> Serialize(Message message)
        {
            var jsonText = JsonConvert.SerializeObject(message.Body, _settings);
            var bytes = _encoding.GetBytes(jsonText);
            var headers = message.Headers.Clone();
            headers[Headers.ContentType] = $"{JsonContentType};charset={_encoding.WebName}";
            return new TransportMessage(headers, bytes);
        }

        /// <summary>
        /// Deserializes the given <see cref="TransportMessage"/> back into a <see cref="Message"/>
        /// </summary>
        public async Task<Message> Deserialize(TransportMessage transportMessage)
        {
            var contentType = transportMessage.Headers.GetValue(Headers.ContentType);

            if (contentType == JsonUtf8ContentType)
            {
                return GetMessage(transportMessage, _encoding);
            }

            if (contentType.StartsWith(JsonContentType))
            {
                var encoding = GetEncoding(contentType);
                return GetMessage(transportMessage, encoding);
            }

            throw new FormatException($"Unknown content type: '{contentType}' - must be '{JsonUtf8ContentType}' for the JSON serialier to work");
        }

        Encoding GetEncoding(string contentType)
        {
            var charset = contentType
                .Split(';')
                .Select(token => token.Split('='))
                .Where(tokens => tokens.Length == 2)
                .FirstOrDefault(tokens => tokens[0] == "charset");

            if (charset == null)
            {
                return _encoding;
            }

            var encodingName = charset[1];

            try
            {
                return Encoding.GetEncoding(encodingName);
            }
            catch (Exception exception)
            {
                throw new FormatException($"Could not turn charset '{encodingName}' into proper encoding!", exception);
            }
        }

        Message GetMessage(TransportMessage transportMessage, Encoding bodyEncoding)
        {
            var bodyString = bodyEncoding.GetString(transportMessage.Body);
            var bodyObject = Deserialize(bodyString);
            var headers = transportMessage.Headers.Clone();
            return new Message(headers, bodyObject);
        }

        object Deserialize(string bodyString)
        {
            try
            {
                return JsonConvert.DeserializeObject(bodyString, _settings);
            }
            catch (Exception exception)
            {
                throw new FormatException($"Could not deserialize JSON text: '{bodyString}'", exception);
            }
        }
    }
}