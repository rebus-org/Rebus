using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Exceptions;
using Rebus.Messages;
using Rebus.Messages.Control;
using Rebus.Pipeline;

namespace Rebus.Legacy
{
    [StepDocumentation("Unpacks the object[] that is always the root object in a legacy message.")]
    class UnpackLegacyMessageIncomingStep : IIncomingStep
    {
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();
            var headers = message.Headers;

            if (headers.ContainsKey(MapLegacyHeadersIncomingStep.LegacyMessageHeader))
            {
                var body = message.Body;
                var array = body as object[];

                if (array == null)
                {
                    throw new FormatException(
                        $"Incoming message has the '{MapLegacyHeadersIncomingStep.LegacyMessageHeader}' header, but the message body {body} is not an object[] as expected");
                }

                foreach (var bodyToDispatch in array)
                {
                    var messageBodyToDispatch = PossiblyConvertBody(bodyToDispatch, headers);

                    context.Save(new Message(headers, messageBodyToDispatch));

                    await next();
                }

                return;
            }

            await next();
        }

        static object PossiblyConvertBody(object messageBody, IReadOnlyDictionary<string, string> headers)
        {
            var legacySubscriptionMessage = messageBody as LegacySubscriptionMessage;

            if (legacySubscriptionMessage == null)
            {
                return messageBody;
            }

            string returnAddress;
            var topic = legacySubscriptionMessage.Type;

            if (!headers.TryGetValue(Headers.ReturnAddress, out returnAddress))
            {
                throw new RebusApplicationException($"Got legacy subscription message but the '{Headers.ReturnAddress}' header was not present on it!");
            }

            var subscribe = legacySubscriptionMessage.Action == 0;

            if (subscribe)
            {
                return new SubscribeRequest
                {
                    Topic = topic,
                    SubscriberAddress = returnAddress
                };
            }

            return new UnsubscribeRequest
            {
                Topic = topic,
                SubscriberAddress = returnAddress
            };
        }
    }
}