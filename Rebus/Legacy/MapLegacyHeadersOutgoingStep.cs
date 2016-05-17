using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Serialization;

namespace Rebus.Legacy
{
    class MapLegacyHeadersOutgoingStep : IOutgoingStep
    {
        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var transportMessage = context.Load<TransportMessage>();

            var body = transportMessage.Body;
            var headers = transportMessage.Headers;

            var newHeaders = MapTrivialHeaders(headers);

            MapSpecialHeaders(newHeaders);

            context.Save(new TransportMessage(newHeaders, body));

            await next();
        }

        void MapSpecialHeaders(Dictionary<string, string> headers)
        {
            MapContentType(headers);
        }

        void MapContentType(Dictionary<string, string> headers)
        {
            string contentType;
            if (!headers.TryGetValue(Headers.ContentType, out contentType)) return;

            if (!contentType.StartsWith(JsonSerializer.JsonContentType))
            {
                throw new FormatException($"Unknown content type: '{contentType}'");
            }

            var encoding = contentType
                .Split(';').Select(token => token.Split('=')).Where(tokens => tokens.Length == 2)
                .Where(tokens => tokens[0] == "charset")
                .Select(tokens => tokens[1])
                .FirstOrDefault();

            if (encoding == null)
            {
                throw new FormatException($"Could not find 'charset' property in the content type: '{contentType}'");
            }

            headers["rebus-content-type"] = "text/json";
            headers["rebus-encoding"] = encoding;
        }

        Dictionary<string,string> MapTrivialHeaders(Dictionary<string, string> headers)
        {
            return headers
                .Select(kvp =>
                {
                    string newKey;

                    return TrivialMappings.TryGetValue(kvp.Key, out newKey)
                        ? new KeyValuePair<string, string>(newKey, kvp.Value)
                        : kvp;
                })
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        static readonly Dictionary<string, string> TrivialMappings = new Dictionary<string, string>
        {
            {"rebus-source-queue", Headers.SourceQueue},
            {"rebus-correlation-id", Headers.CorrelationId},
            {"rebus-return-address", Headers.ReturnAddress},
            {"rebus-msg-id", Headers.MessageId},
            
            //{"rebus-encrypted", EncryptionHeaders.ContentEncryption},
            //{"rebus-salt", iv},
            //{"rebus-rijndael", Headers.ContentEncoding},
        }.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    }
}