using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Pipeline;

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

            context.Save(new TransportMessage(headers, body));

            await next();
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