using System;
using System.Threading.Tasks;
using Rebus.Messages;

namespace Rebus.Pipeline.Send
{
    public class AssignDateTimeOffsetHeader : IOutgoingStep
    {
        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();
            var headers = message.Headers;

            headers[Headers.SentTime] = DateTimeOffset.UtcNow.ToString("O");

            await next();
        }
    }
}