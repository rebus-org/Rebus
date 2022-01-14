using System;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;

namespace Rebus.Handlers.Reordering;

/// <summary>
/// Incoming message step that can reorder handlers
/// </summary>
public class HandlerReorderingStep : IIncomingStep
{
    readonly ReorderingConfiguration _configuration;

    /// <summary>
    /// Constructs the step with the given configuration
    /// </summary>
    public HandlerReorderingStep(ReorderingConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Reorders the handler invokers if necessary
    /// </summary>
    public async Task Process(IncomingStepContext context, Func<Task> next)
    {
        var handlerInvokers = context.Load<HandlerInvokers>();
        var orderedHandlerInvokers = handlerInvokers.OrderBy(i => _configuration.GetIndex(i.Handler));
        var newHandlerInvokers = new HandlerInvokers(handlerInvokers.Message, orderedHandlerInvokers);
            
        context.Save(newHandlerInvokers);
            
        await next();
    }
}