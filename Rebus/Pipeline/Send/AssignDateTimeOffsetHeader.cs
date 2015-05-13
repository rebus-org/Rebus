using System;
using System.Threading.Tasks;
using Rebus.Messages;

namespace Rebus.Pipeline.Send
{
    /// <summary>
    /// Outgoing step that sets the <see cref="Headers.SentTime"/> header of the outgoing message to <see cref="DateTimeOffset.Now"/>
    /// (serializing it with the "O" format, i.e. its ISO 8601 representation)
    /// </summary>
    public class AssignDateTimeOffsetHeader : IOutgoingStep
    {
        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();
            var headers = message.Headers;

            headers[Headers.SentTime] = DateTimeOffset.Now.ToString("O");

            await next();
        }
    }
}