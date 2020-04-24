using System;
using System.Threading.Tasks;
using Rebus.Bus.Advanced;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Retry.FailFast;

#pragma warning disable 1998

namespace Rebus.Exceptions
{
    /// <summary>
    /// Extensions for <see cref="ITransportMessageApi"/>
    /// </summary>
    public static class TransportMessageApiExtensions
    {
        /// <summary>
        /// Manually dead-letters the message currently being handled. Optionally passes the given <paramref name="errorDetails"/> along as the <see cref="Headers.ErrorDetails"/> header.
        /// </summary>
        public static async Task Deadletter(this ITransportMessageApi transportMessageApi, string errorDetails = null, bool throwIfAlreadyDeadlettered = false)
        {
            if (transportMessageApi == null) throw new ArgumentNullException(nameof(transportMessageApi));

            var context = MessageContext.Current ?? throw new InvalidOperationException($"Attempted to dead-letter the current message using error details '{errorDetails}', but no message context could be found! This is probably a sign that this method was called OUTSIDE of a Rebus handler, or on a separate, disconnected thread somehow. Please only call this method inside Rebus handlers.");

            if (throwIfAlreadyDeadlettered)
            {
                
            }

            context.IncomingStepContext.Save(new ManualDeadletterCommand(errorDetails));
        }
    }
}