using System;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.Legacy
{
    class PackLegacyMessageOutgoingStep : IOutgoingStep
    {
        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();
            var headers = message.Headers;
            var body = new[]{message.Body};
            
            context.Save(new Message(headers, body));

            await next();
        }
    }
}