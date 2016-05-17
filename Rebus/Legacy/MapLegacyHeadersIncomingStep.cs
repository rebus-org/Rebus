using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Serialization;

namespace Rebus.Legacy
{
    [StepDocumentation("Mutates the headers of the incoming message by mapping understood Rebus1 headers to their counterparts in Rebus2")]
    class MapLegacyHeadersIncomingStep : IIncomingStep
    {
        internal const string LegacyMessageHeader = "rbs2-rebus-legacy-message";

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var transportMessage = context.Load<TransportMessage>();
            var headers = transportMessage.Headers;

            if (headers.ContainsKey("rebus-msg-id"))
            {
                MutateLegacyTransportMessage(context, headers, transportMessage);
            }

            await next();
        }

        void MutateLegacyTransportMessage(IncomingStepContext context, Dictionary<string, string> headers, TransportMessage transportMessage)
        {
            var newHeaders = MapTrivialHeaders(headers);

            MapSpecialHeaders(newHeaders);

            newHeaders[LegacyMessageHeader] = "";

            context.Save(new TransportMessage(newHeaders, transportMessage.Body));
        }

        void MapSpecialHeaders(Dictionary<string, string> headers)
        {
            MapContentType(headers);
        }

        static void MapContentType(Dictionary<string, string> headers)
        {
            string contentType;
            if (!headers.TryGetValue("rebus-content-type", out contentType)) return;

            if (contentType == "text/json")
            {
                string contentEncoding;

                if (headers.TryGetValue("rebus-encoding", out contentEncoding))
                {
                    headers.Remove("rebus-content-type");
                    headers.Remove("rebus-encoding");

                    headers[Headers.ContentType] = $"{JsonSerializer.JsonContentType};charset={contentEncoding}";
                }
                else
                {
                    throw new FormatException(
                        "Content type was 'text/json', but the 'rebus-encoding' header was not present!");
                }
            }
            else
            {
                throw new FormatException(
                    $"Sorry, but the '{contentType}' content type is currently not supported by the legacy header mapper");
            }
        }

        Dictionary<string, string> MapTrivialHeaders(Dictionary<string, string> headers)
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
        };
    }
}