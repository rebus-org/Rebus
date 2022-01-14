using System;
using System.Threading;
using Rebus.Pipeline;

namespace Rebus.Extensions;

/// <summary>
/// Convenient extensions for working with the message context
/// </summary>
public static class MessageContextExtensions
{
    /// <summary>
    /// Gets the bus' shutdown <see cref="CancellationToken"/>. Can be used when implementing long-running
    /// message handlers to avoid blocking bus shutdown, either by periodically checking <see cref="CancellationToken.IsCancellationRequested"/>
    /// or calling <see cref="CancellationToken.ThrowIfCancellationRequested"/>, or by passing it to asynchronous operations that support
    /// the cancellation pattern.
    /// </summary>
    public static CancellationToken GetCancellationToken(this IMessageContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
            
        var incomingStepContext = context.IncomingStepContext;
            
        return incomingStepContext.Load<CancellationToken>();
    }
}