using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Messages;

namespace Rebus.Pipeline.Send;

/// <summary>
/// Outgoing message step that checks the consistency of the message
/// </summary>
[StepDocumentation("Checks the consistency of the outgoing message")]
public class ValidateOutgoingMessageStep : IOutgoingStep
{
    /// <summary>
    /// Executes the step
    /// </summary>
    public async Task Process(OutgoingStepContext context, Func<Task> next)
    {
        var message = context.Load<TransportMessage>();
        var headers = message.Headers;

        CheckDeferHeaders(headers);

        await next();
    }

    static void CheckDeferHeaders(IReadOnlyDictionary<string, string> headers)
    {
        // no problemo
        if (!headers.ContainsKey(Headers.DeferredUntil)) return;

        // recipient must have been set by now
        if (!headers.TryGetValue(Headers.DeferredRecipient, out var destinationAddress)
            || destinationAddress == null)
        {
            throw new InvalidOperationException(
                $"When you defer a message from a one-way client, you need to explicitly set the '{Headers.DeferredRecipient}' header in order to specify a recipient for the message when it is time to be delivered");
        }
    }
}