using System;
using Rebus.Pipeline.Receive;

namespace Rebus.Pipeline;

/// <summary>
/// Extensions for the message context 
/// </summary>
public static class PipelineMessageContextExtensions
{
    /// <summary>
    /// Aborts the current message processing pipeline by making the currently executing handler the last one.
    /// This means that any message handlers following the current one in the current pipeline will NOT be executed.
    /// If no errors occurred, the queue transaction will be committed as if everything is allright.
    /// This method can be used to ABORT message process to allow for a handler to FUNCTION AS A FILTER.
    /// </summary>
    public static void AbortDispatch(this IMessageContext messageContext)
    {
        if (messageContext == null) throw new ArgumentNullException(nameof(messageContext));

        messageContext.IncomingStepContext.Save(DispatchIncomingMessageStep.AbortDispatchContextKey, true);
    }
}