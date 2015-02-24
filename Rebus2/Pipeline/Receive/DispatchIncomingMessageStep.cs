using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rebus2.Pipeline.Receive
{
    public class DispatchIncomingMessageStep : IIncomingStep
    {
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var invokers = context.Load<List<HandlerInvoker>>();

            foreach (var invoker in invokers)
            {
                await invoker.Invoke();
            }

            await next();
        }
    }
}