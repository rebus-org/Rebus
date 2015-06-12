using System;
using System.Threading.Tasks;
using Rebus.Messages;

namespace Rebus.Pipeline.Send
{
    /// <summary>
    /// Outgoing step that sets the <see cref="Headers.MessageId"/> header of the outgoing message, unless it has already been set.
    /// </summary>
    [StepDocumentation("Assigns a new GUID as the '" + Headers.MessageId + "' header if the message being sent does not already have a message ID.")]
    public class AssignGuidMessageIdStep : IOutgoingStep
    {
        /// <summary>
        /// Sets the <see cref="Headers.MessageId"/>. The message ID is a new <see cref="Guid"/>
        /// </summary>
        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();
            var headers = message.Headers;

            if (!headers.ContainsKey(Headers.MessageId))
            {
                headers[Headers.MessageId] = Guid.NewGuid().ToString();
            }

            await next();
        }
    }
}