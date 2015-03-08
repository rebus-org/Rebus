using System;
using Rebus.Messages;
using Rebus.Pipeline.Receive;
using Rebus.Transport;

namespace Rebus.Bus
{
    public static class MessageExtensions
    {
        public static bool HasReturnAddress(this Message message)
        {
            return message.Headers.ContainsKey(Headers.ReturnAddress);
        }

        public static void SetReturnAddressFromTransport(this Message message, ITransport transport)
        {
            var returnAddress = transport.Address;

            if (string.IsNullOrWhiteSpace(returnAddress))
            {
                throw new InvalidOperationException("Cannot set return address from the given transport because it is not capable of receiving messages");
            }

            message.Headers[Headers.ReturnAddress] = returnAddress;
        }

        public static void SetDeferHeader(this Message message, DateTimeOffset approximateDeliveryTime)
        {
            message.Headers[Headers.DeferredUntil] = approximateDeliveryTime.ToString(HandleDeferredMessagesStep.DateTimeOffsetFormat);
        }
    }
}