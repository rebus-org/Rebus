using System;
using System.IO;
using Microsoft.ServiceBus.Messaging;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Messages;

namespace Rebus.AzureServiceBus
{
    static class MsgHelpers
    {
        public static BrokeredMessage CreateBrokeredMessage(TransportMessage message)
        {
            var headers = message.Headers.Clone();
            var brokeredMessage = new BrokeredMessage(new MemoryStream(message.Body), true);

            string timeToBeReceivedStr;
            if (headers.TryGetValue(Headers.TimeToBeReceived, out timeToBeReceivedStr))
            {
                timeToBeReceivedStr = headers[Headers.TimeToBeReceived];
                var timeToBeReceived = TimeSpan.Parse(timeToBeReceivedStr);
                brokeredMessage.TimeToLive = timeToBeReceived;
            }

            string deferUntilTime;
            if (headers.TryGetValue(Headers.DeferredUntil, out deferUntilTime))
            {
                var deferUntilDateTimeOffset = deferUntilTime.ToDateTimeOffset();
                brokeredMessage.ScheduledEnqueueTimeUtc = deferUntilDateTimeOffset.UtcDateTime;
                headers.Remove(Headers.DeferredUntil);
            }

            string contentType;
            if (headers.TryGetValue(Headers.ContentType, out contentType))
            {
                brokeredMessage.ContentType = contentType;
            }

            string correlationId;
            if (headers.TryGetValue(Headers.CorrelationId, out correlationId))
            {
                brokeredMessage.CorrelationId = correlationId;
            }

            brokeredMessage.Label = message.GetMessageLabel();

            foreach (var kvp in headers)
            {
                brokeredMessage.Properties[kvp.Key] = PossiblyLimitLength(kvp.Value);
            }

            return brokeredMessage;
        }

        static string PossiblyLimitLength(string str)
        {
            const int maxLengthPrettySafe = 16300;

            if (str.Length < maxLengthPrettySafe) return str;

            var firstPart = str.Substring(0, 8000);
            var lastPart = str.Substring(str.Length - 8000);

            return $"{firstPart} (... cut out because length exceeded {maxLengthPrettySafe} characters ...) {lastPart}";
        }
    }
}