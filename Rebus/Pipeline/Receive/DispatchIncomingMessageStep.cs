using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Messages;

namespace Rebus.Pipeline.Receive
{
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