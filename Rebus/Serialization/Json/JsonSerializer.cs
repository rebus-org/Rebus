using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rebus.Extensions;
using Rebus.Messages;

#pragma warning disable 1998

namespace Rebus.Serialization.Json
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
        readonly IMessageTypeNameConvention _messageTypeNameConvention;
        readonly string _encodingHeaderValue;

        public JsonSerializer(IMessageTypeNameConvention messageTypeNameConvention, JsonSerializerSettings jsonSerializerSettings = null, Encoding encoding = null)
        {
            _messageTypeNameConvention = messageTypeNameConvention ?? throw new ArgumentNullException(nameof(messageTypeNameConvention));
            _settings = jsonSerializerSettings ?? DefaultSettings;
            _encoding = encoding ?? DefaultEncoding;

            _encodingHeaderValue = $"{JsonContentType};charset={_encoding.HeaderName}";
        }

        /// <summary>
        /// Serializes the given <see cref="Message"/> into a <see cref="TransportMessage"/>
        /// </summary>
        public async Task<TransportMessage> Serialize(Message message)
        {
            var jsonText = JsonConvert.SerializeObject(message.Body, _settings);
            var bytes = _encoding.GetBytes(jsonText);
            var headers = message.Headers.Clone();

            headers[Headers.ContentType] = _encodingHeaderValue;

            if (!headers.ContainsKey(Headers.Type))
            {
                headers[Headers.Type] = _messageTypeNameConvention.GetTypeName(message.Body.GetType());
            }

            return new TransportMessage(headers, bytes);
        }

        /// <summary>
        /// Deserializes the given <see cref="TransportMessage"/> back into a <see cref="Message"/>
        /// </summary>
        public async Task<Message> Deserialize(TransportMessage transportMessage)
        {
            var contentType = transportMessage.Headers.GetValue(Headers.ContentType);

            if (contentType.Equals(JsonUtf8ContentType, StringComparison.OrdinalIgnoreCase))
            {
                return GetMessage(transportMessage, _encoding);
            }

            if (contentType.StartsWith(JsonContentType))
            {
                var encoding = GetEncoding(contentType);
                return GetMessage(transportMessage, encoding);
            }

            throw new FormatException($"Unknown content type: '{contentType}' - must be '{JsonContentType}' (e.g. '{JsonUtf8ContentType}') for the JSON serialier to work");
        }

        Encoding GetEncoding(string contentType)
        {
            var parts = contentType.Split(';');

            var charset = parts
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
            var type = GetTypeOrNull(transportMessage);
            var bodyObject = Deserialize(bodyString, type);
            var headers = transportMessage.Headers.Clone();
            return new Message(headers, bodyObject);
        }

        Type GetTypeOrNull(TransportMessage transportMessage)
        {
            if (!transportMessage.Headers.TryGetValue(Headers.Type, out var typeName)) return null;

            var type = _messageTypeNameConvention.GetType(typeName) ?? throw new FormatException($"Could not get .NET type named '{typeName}'");

            return type;
        }

        object Deserialize(string bodyString, Type type)
        {
            try
            {
                return type == null
                    ? JsonConvert.DeserializeObject(bodyString, _settings)
                    : JsonConvert.DeserializeObject(bodyString, type, _settings);
            }
            catch (Exception exception)
            {
                if (bodyString.Length > 32768)
                {
                    throw new FormatException($"Could not deserialize JSON text (original length: {bodyString.Length}): '{Limit(bodyString, 5000)}'", exception);
                }

                throw new FormatException($"Could not deserialize JSON text: '{bodyString}'", exception);
            }
        }

        static string Limit(string bodyString, int maxLength) => string.Concat(bodyString.Substring(0, maxLength), " (......)");
    }
}