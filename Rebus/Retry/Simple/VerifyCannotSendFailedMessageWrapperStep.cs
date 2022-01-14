using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.Retry.Simple;

[StepDocumentation("When 2nd level retries are enabled, it is easy to accidentally Send/Defer the incoming IFailed<TMessage> instead of the TMessage. This step prohibits that.")]
class VerifyCannotSendFailedMessageWrapperStep : IOutgoingStep
{
    public async Task Process(OutgoingStepContext context, Func<Task> next)
    {
        var message = context.Load<Message>();

        var body = message.Body;

        if (body != null)
        {
            var interfaces = body.GetType().GetInterfaces();

            if (interfaces.Any(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IFailed<>)))
            {
                throw new InvalidOperationException($"Tried to send {body} - it is not allowed to send an IFailed<TMessage> anywhere because a) it most likely is an error, because you accidentally called Send/Defer with 'failed' and not with 'failed.Message' as the argument, and b) it could confuse things a lot - we like to avoid confusing things");
            }
        }

        await next();
    }
}