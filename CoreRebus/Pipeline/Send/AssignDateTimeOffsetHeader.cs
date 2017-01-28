using System;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Time;

namespace Rebus.Pipeline.Send
{
    /// <summary>
    /// Outgoing step that sets the <see cref="Headers.SentTime"/> header of the outgoing message to <see cref="RebusTime.Now"/>
    /// (serializing it with the "O" format, i.e. its ISO 8601 representation)
    /// </summary>
    [StepDocumentation("Sets the '" + Headers.SentTime + "' header of the outgoing message to the current local time as a DateTimeOffset serialized with the 'O' format string.")]
    public class AssignDateTimeOffsetHeader : IOutgoingStep
    {
        /// <summary>
        /// Sets the <see cref="Headers.SentTime"/> header
        /// </summary>
        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();
            var headers = message.Headers;

            headers[Headers.SentTime] = RebusTime.Now.ToString("O");

            await next();
        }
    }
}