using System;
using Rebus.Messages;
using Rebus.Pipeline.Receive;
using Rebus.Transport;

namespace Rebus.Bus
{
    /// <summary>
    /// Small helpers that make it easier to work with the <see cref="Message"/> class
    /// </summary>
    public static class MessageExtensions
    {
        /// <summary>
        /// Gets whether the message's <see cref="Headers.ReturnAddress"/> header is set to something
        /// </summary>
        public static bool HasReturnAddress(this Message message)
        {
            return message.Headers.ContainsKey(Headers.ReturnAddress);
        }

        /// <summary>
        /// Uses the transport's input queue address as the <see cref="Headers.ReturnAddress"/> on the message
        /// </summary>
        public static void SetReturnAddressFromTransport(this Message message, ITransport transport)
        {
            var returnAddress = transport.Address;

            if (string.IsNullOrWhiteSpace(returnAddress))
            {
                throw new InvalidOperationException("Cannot set return address from the given transport because it is not capable of receiving messages");
            }

            message.Headers[Headers.ReturnAddress] = returnAddress;
        }

        /// <summary>
        /// Sets the <see cref="Headers.DeferredUntil"/> header to the specified time
        /// </summary>
        public static void SetDeferHeader(this Message message, DateTimeOffset approximateDeliveryTime)
        {
            message.Headers[Headers.DeferredUntil] = approximateDeliveryTime.ToString(HandleDeferredMessagesStep.DateTimeOffsetFormat);
        }
    }
}