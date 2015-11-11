using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Messages;

namespace Rebus.Pipeline.Receive
{
    /// <summary>
    /// Incoming step that gets a <see cref="List{T}"/> where T is <see cref="HandlerInvoker"/> from the context
    /// and invokes them in the order they're in.
    /// </summary>
    [StepDocumentation(@"Gets all the handler invokers from the current context and invokes them in order.

Please note that each invoker might choose to ignore the invocation internally.

If no invokers were found, a RebusApplicationException is thrown.")]
    public class DispatchIncomingMessageStep : IIncomingStep
    {
        /// <summary>
        /// Keys of an <see cref="IncomingStepContext"/> items that indicates that message dispatch must be stopped
        /// </summary>
        public const string AbortDispatchContextKey = "abort-dispatch-to-handlers";

        /// <summary>
        /// Processes the message
        /// </summary>
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var invokers = context.Load<HandlerInvokers>();
            var didInvokeHandler = false;

            // if dispatch has already been aborted (e.g. in a transport message filter or something else that
            // was run before us....) bail out here:
            if (context.Load<bool>(AbortDispatchContextKey))
            {
                await next();
                return;
            }

            foreach (var invoker in invokers)
            {
                await invoker.Invoke();
                didInvokeHandler = true;

                // if dispatch was aborted at this point, bail out
                if (context.Load<bool>(AbortDispatchContextKey)) break;
            }

            // throw error if we should have executed a handler but we didn't
            if (!didInvokeHandler)
            {
                var message = context.Load<Message>();
                
                var messageId = message.GetMessageId();
                var messageType = message.GetMessageType();

                var text = string.Format("Message with ID {0} and type {1} could not be dispatched to any handlers",
                    messageId, messageType);

                throw new RebusApplicationException(text);
            }

            await next();
        }
    }
}