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
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var transportMessage = context.Load<TransportMessage>();
            var headers = transportMessage.Headers;
            
            var newHeaders = MapSpecialHeaders(MapTrivialHeaders(headers));

            context.Save(new TransportMessage(newHeaders, transportMessage.Body));

            await next();
        }

        Dictionary<string, string> MapSpecialHeaders(Dictionary<string, string> headers)
        {
            return headers
                .Select(kvp =>
                {
                    Func<KeyValuePair<string, string>, KeyValuePair<string, string>> mapper;

                    return SpecialMappings.TryGetValue(kvp.Key, out mapper)
                        ? mapper(kvp)
                        : kvp;
                })
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
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

        static readonly Dictionary<string, Func<KeyValuePair<string,string>, KeyValuePair<string,string>>> SpecialMappings = new Dictionary<string, Func<KeyValuePair<string, string>, KeyValuePair<string, string>>>
        {
            {"rebus-content-type", MapContentType}
        };

        static KeyValuePair<string, string> MapContentType(KeyValuePair<string, string> header)
        {
            return header.Value == "text/json" 
                ? new KeyValuePair<string, string>(Headers.ContentType, JsonSerializer.JsonUtf8ContentType) 
                : header;
        }

        static readonly Dictionary<string, string> TrivialMappings = new Dictionary<string, string>
        {
            {"rebus-source-queue", Headers.SourceQueue},
            {"rebus-correlation-id", Headers.CorrelationId},
            {"rebus-return-address", Headers.ReturnAddress},
            {"rebus-msg-id", Headers.MessageId},
        };
    }
}