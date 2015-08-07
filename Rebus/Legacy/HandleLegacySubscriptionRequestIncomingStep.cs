using System;
using System.Text;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Subscriptions;

namespace Rebus.Legacy
{
    class HandleLegacySubscriptionRequestIncomingStep : IIncomingStep
    {
        readonly ISubscriptionStorage _subscriptionStorage;
        readonly LegacySubscriptionMessageSerializer _legacySubscriptionMessageSerializer;

        public HandleLegacySubscriptionRequestIncomingStep(ISubscriptionStorage subscriptionStorage, LegacySubscriptionMessageSerializer legacySubscriptionMessageSerializer)
        {
            _subscriptionStorage = subscriptionStorage;
            _legacySubscriptionMessageSerializer = legacySubscriptionMessageSerializer;
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var transportMessage = context.Load<TransportMessage>();

            if (!await CouldHandleLegacySubscriptionRequest(transportMessage))
            {
                await next();
            }
        }

        async Task<bool> CouldHandleLegacySubscriptionRequest(TransportMessage transportMessage)
        {
            var headers = transportMessage.Headers;
            var body = transportMessage.Body;

            if (body == null) return false;

            string rebusContentType;
            string rebusEncoding;
            string returnAddress;

            if (!headers.TryGetValue("rebus-content-type", out rebusContentType)) return false;

            if (rebusContentType != "text/json") return false;
            
            if (!headers.TryGetValue("rebus-encoding", out rebusEncoding)) return false;

            if (!headers.TryGetValue("rebus-return-address", out returnAddress)) return false;

            var encoding = Encoding.GetEncoding(rebusEncoding);

            var jsonText = encoding.GetString(transportMessage.Body);

            if (!jsonText.Contains("Rebus.Messages.SubscriptionMessage")) return false;

            try
            {
                var legacySubscriptionMessage = _legacySubscriptionMessageSerializer.GetAsLegacySubscriptionMessage(jsonText);
                var subscribe = legacySubscriptionMessage.Action == 0;

                if (subscribe)
                {
                    await _subscriptionStorage.RegisterSubscriber(legacySubscriptionMessage.Type, returnAddress);
                }
                else
                {
                    await _subscriptionStorage.UnregisterSubscriber(legacySubscriptionMessage.Type, returnAddress);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}