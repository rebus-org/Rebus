using System;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.Routing.TransportMessages;

/// <summary>
/// Incoming message step that looks at the transport message and possibly forwards it to another queue
/// </summary>
[StepDocumentation("This step allows for very quickly forwarding of the incoming transport message without performing any further actions")]
public class ForwardTransportMessageStep : IIncomingStep
{
    readonly Func<TransportMessage, Task<ForwardAction>> _routingFunction;
    readonly ITransport _transport;
    readonly string _errorQueueName;
    readonly ErrorBehavior _errorBehavior;
    readonly ILog _log;

    /// <summary>
    /// Constructs the step
    /// </summary>
    public ForwardTransportMessageStep(Func<TransportMessage, Task<ForwardAction>> routingFunction, ITransport transport, IRebusLoggerFactory rebusLoggerFactory, string errorQueueName, ErrorBehavior errorBehavior)
    {
        if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));
        _routingFunction = routingFunction ?? throw new ArgumentNullException(nameof(routingFunction));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _errorQueueName = errorQueueName;
        _errorBehavior = errorBehavior;
        _log = rebusLoggerFactory.GetLogger<ForwardTransportMessageStep>();
    }

    /// <summary>
    /// Invokes the routing function and performs some action depending on the returned <see cref="ForwardAction"/> result
    /// </summary>
    public async Task Process(IncomingStepContext context, Func<Task> next)
    {
        var transportMessage = context.Load<TransportMessage>();

        try
        {
            var routingResult = await _routingFunction(transportMessage) ?? ForwardAction.None;
            var actionType = routingResult.ActionType;

            switch (actionType)
            {
                case ActionType.Forward:
                    var destinationAddresses = routingResult.DestinationAddresses;
                    var transactionContext = context.Load<ITransactionContext>();

                    _log.Debug("Forwarding {messageLabel} to {queueNames}", transportMessage.GetMessageLabel(), destinationAddresses);

                    await Task.WhenAll(destinationAddresses
                        .Select(address => _transport.Send(address, transportMessage, transactionContext)));
                    break;

                case ActionType.None:
                    await next();
                    break;

                case ActionType.Ignore:
                    _log.Debug("Ignoring {messageLabel}", transportMessage.GetMessageLabel());
                    break;

                default:
                    throw new ArgumentException($"Unknown forward action type: {actionType}");
            }
        }
        catch (Exception e2)
        {
            if (_errorBehavior == ErrorBehavior.ForwardToErrorQueue)
            {
                transportMessage.Headers[Headers.SourceQueue] = _transport.Address;
                transportMessage.Headers[Headers.ErrorDetails] = e2.ToString();

                try
                {
                    var transactionContext = context.Load<ITransactionContext>();
                    await _transport.Send(_errorQueueName, transportMessage, transactionContext);
                    return;
                }
                catch (Exception exception)
                {
                    _log.Error(exception, "Could not forward message {messageLabel} to {queueName} - waiting 5 s", transportMessage.GetMessageLabel(), _errorQueueName);
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    context.Load<ITransactionContext>().Abort();
                }
            }

            throw;
        }
    }
}