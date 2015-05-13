using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Messages;

namespace Rebus.Pipeline.Receive
{
    /// <summary>
    /// Incoming step that gets a <see cref="List{T}"/> where T is <see cref="HandlerInvoker"/> from the context
    /// and invokes them in the order they're in.
    /// </summary>
    public class DispatchIncomingMessageStep : IIncomingStep
    {
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var invokers = context.Load<List<HandlerInvoker>>();
            var didInvokeHandler = false;

            foreach (var invoker in invokers)
            {
                await invoker.Invoke();
                didInvokeHandler = true;
            }

            if (!didInvokeHandler)
            {
                throw new ApplicationException(string.Format("Message with ID {0} could not be dispatched to any handlers", 
                    context.Load<Message>().Headers.GetValue(Headers.MessageId)));
            }

            await next();
        }
    }
}