using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Messages.Control;
using Rebus.Pipeline;

namespace Rebus.Legacy
{
    [StepDocumentation("Packs the logical message into the object[] that is always the root object in a legacy message.")]
    class PackLegacyMessageOutgoingStep : IOutgoingStep
    {
        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();
            var headers = message.Headers;
            var messageBodyToSend = PossiblyConvertBody(headers, message.Body);
            var body = new[]{messageBodyToSend};
            
            context.Save(new Message(headers, body));

            await next();
        }

        static object PossiblyConvertBody(IDictionary<string, string> headers, object messageBody)
        {
            var subscribeRequest = messageBody as SubscribeRequest;
            if (subscribeRequest != null)
            {
                headers[Headers.ReturnAddress] = subscribeRequest.SubscriberAddress;

                return new LegacySubscriptionMessage
                {
                    Type = subscribeRequest.Topic,
                    Action = 0 //< subscribe
                };
            }

            var unsubscribeRequest = messageBody as UnsubscribeRequest;
            if (unsubscribeRequest != null)
            {
                headers[Headers.ReturnAddress] = unsubscribeRequest.SubscriberAddress;

                return new LegacySubscriptionMessage
                {
                    Type = unsubscribeRequest.Topic,
                    Action = 1 //< unsubscribe
                };
            }

            return messageBody;
        }
    }
}