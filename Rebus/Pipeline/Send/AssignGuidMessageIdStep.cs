using System;
using System.Threading.Tasks;
using Rebus.Messages;

namespace Rebus.Pipeline.Send
{
    public class AssignGuidMessageIdStep : IOutgoingStep
    {
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