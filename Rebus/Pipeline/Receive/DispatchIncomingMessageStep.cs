using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Logging;
// ReSharper disable ForCanBeConvertedToForeach

namespace Rebus.Pipeline.Receive;

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
        if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));
        _log = rebusLoggerFactory.GetLogger<DispatchIncomingMessageStep>();
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
        var message = invokers.Message;

        var messageId = message.GetMessageId();
        var messageType = message.GetMessageType();

        // if dispatch has already been aborted (e.g. in a transport message filter or something else that
        // was run before us....) bail out here:
        if (context.Load<bool>(AbortDispatchContextKey))
        {
            _log.Debug("Skipping dispatch of message {messageType} {messageId}", messageType, messageId);
            await next();
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        for(var index = 0; index < invokers.Count; index++)
        {
            var invoker = invokers[index];

            await invoker.Invoke();
            handlersInvoked++;

            // if dispatch was aborted at this point, bail out
            if (context.Load<bool>(AbortDispatchContextKey))
            {
                _log.Debug("Skipping further dispatch of message {messageType} {messageId}", messageType, messageId);
                break;
            }
        }

        // throw error if we should have executed a handler but we didn't
        if (handlersInvoked == 0)
        {
            var text = $"Message with ID {messageId} and type {messageType} could not be dispatched to any handlers (and will not be retried under the default fail-fast settings)";

            throw new MessageCouldNotBeDispatchedToAnyHandlersException(text);
        }

        _log.Debug("Dispatching {messageType} {messageId} to {count} handlers took {elapsedMs:0} ms", 
            messageType, messageId, handlersInvoked, stopwatch.ElapsedMilliseconds);

        await next();
    }
}