using System;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Messages;

namespace Rebus.Pipeline.Send
{
    /// <summary>
    /// Outgoing step that sets the <see cref="Headers.Type"/> header of the outgoing message, unless it has already been set.
    /// </summary>
    [StepDocumentation("Assigns the simple assembly-qualified type name of the sent object as the '" + Headers.Type + "' header, unless it has already been set.")]
    public class AssignTypeHeaderStep : IOutgoingStep
    {
        /// <summary>
        /// Sets the <see cref="Headers.Type"/> to the simple assembly-qualified type name of the sent object, unless
        /// the header has not already been added
        /// </summary>
        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();
            var headers = message.Headers;
            var messageType = message.Body.GetType();

            if (!headers.ContainsKey(Headers.Type))
            {
                headers[Headers.Type] = messageType.GetSimpleAssemblyQualifiedName();
            }

            await next();
        }
    }
}