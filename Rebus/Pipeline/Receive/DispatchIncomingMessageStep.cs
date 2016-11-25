using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Logging;
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
        readonly ILog _log;

        /// <summary>
        /// Creates the step
        /// </summary>
        public DispatchIncomingMessageStep(IRebusLoggerFactory rebusLoggerFactory)
        {
            _log = rebusLoggerFactory.GetCurrentClassLogger();
        }

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
            var handlersInvoked = 0;
            var messageLabel = invokers.Message.GetMessageLabel();

            // if dispatch has already been aborted (e.g. in a transport message filter or something else that
            // was run before us....) bail out here:
            if (context.Load<bool>(AbortDispatchContextKey))
            {
                _log.Debug("Skipping dispatch of message {0}", messageLabel);
                await next();
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            foreach (var invoker in invokers)
            {
                await invoker.Invoke();
                handlersInvoked++;

                // if dispatch was aborted at this point, bail out
                if (context.Load<bool>(AbortDispatchContextKey))
                {
                    _log.Debug("Skipping further dispatch of message {0}", messageLabel);
                    break;
                }
            }

            // throw error if we should have executed a handler but we didn't
            if (handlersInvoked == 0)
            {
                var message = context.Load<Message>();
                
                var messageId = message.GetMessageId();
                var messageType = message.GetMessageType();

                var text = $"Message with ID {messageId} and type {messageType} could not be dispatched to any handlers";

                throw new RebusApplicationException(text);
            }

            _log.Debug("Dispatching message {0} to {1} handlers took {2:0} ms", 
                messageLabel, handlersInvoked, stopwatch.Elapsed.TotalMilliseconds);

            await next();
        }
    }
}