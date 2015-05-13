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
            var invokers = context.Load<HandlerInvokers>();
            var didInvokeHandler = false;

            foreach (var invoker in invokers)
            {
                await invoker.Invoke();
                didInvokeHandler = true;
            }

            if (!didInvokeHandler)
            {
                var message = context.Load<Message>();
                var headers = message.Headers;
                
                var messageId = headers.GetValue(Headers.MessageId);
                var messageType = headers.GetValueOrNull(Headers.Type) ?? "<unknown>";

                var text = string.Format("Message with ID {0} and type {1} could not be dispatched to any handlers",
                    messageId, messageType);

                throw new ApplicationException(text);
            }

            await next();
        }
    }
}