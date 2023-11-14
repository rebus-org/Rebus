using System;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Retry;
using Rebus.Transport;

namespace Rebus.Routing.TransportMessages;

/// <summary>
/// Incoming message step that looks at the transport message and possibly forwards it to another queue
/// </summary>
[StepDocumentation("This step allows for very quickly forwarding of the incoming transport message without performing any further actions")]
public class ForwardTransportMessageStep : IIncomingStep
{
    readonly Func<TransportMessage, Task<ForwardAction>> _routingFunction;
    readonly ErrorBehavior _errorBehavior;
    readonly IErrorHandler _errorHandler;
    readonly ITransport _transport;
    readonly ILog _log;

    /// <summary>
    /// Constructs the step
    /// </summary>
    public ForwardTransportMessageStep(Func<TransportMessage, Task<ForwardAction>> routingFunction, ITransport transport, IRebusLoggerFactory rebusLoggerFactory, ErrorBehavior errorBehavior, IErrorHandler errorHandler)
    {
        if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));
        _routingFunction = routingFunction ?? throw new ArgumentNullException(nameof(routingFunction));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _errorBehavior = errorBehavior;
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
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
                case ActionType.None:
                    await next();
                    break;

                case ActionType.Forward:
                    {
                        var destinationAddresses = routingResult.DestinationQueueNames;
                        var transactionContext = context.Load<ITransactionContext>();

                        _log.Debug("Forwarding {messageLabel} to {queueNames}", transportMessage.GetMessageLabel(), destinationAddresses);
                        await Task.WhenAll(destinationAddresses.Select(address => _transport.Send(address, transportMessage, transactionContext)));

                        transactionContext.SetResult(commit: true, ack: true);
                        await CommitIfPossible(transactionContext);

                        break;
                    }

                case ActionType.Ignore:
                    {
                        var transactionContext = context.Load<ITransactionContext>();

                        _log.Debug("Ignoring {messageLabel}", transportMessage.GetMessageLabel());

                        transactionContext.SetResult(commit: true, ack: true);
                        await CommitIfPossible(transactionContext);

                        break;
                    }

                default:
                    throw new ArgumentException($"Unknown forward action type: {actionType}");
            }
        }
        catch (Exception exception)
        {
            var transactionContext = context.Load<ITransactionContext>();

            if (_errorBehavior == ErrorBehavior.Normal)
            {
                transportMessage.Headers[Headers.SourceQueue] = _transport.Address;
                transportMessage.Headers[Headers.ErrorDetails] = exception.ToString();

                try
                {
                    transactionContext.SetResult(commit: false, ack: true);
                    using var scope = new RebusTransactionScope();
                    await _errorHandler.HandlePoisonMessage(transportMessage, scope.TransactionContext, ExceptionInfo.FromException(exception));
                    await scope.CompleteAsync();
                    return;
                }
                catch (Exception exception2)
                {
                    transactionContext.SetResult(commit: false, ack: false);
                    _log.Error(exception2, "Error when passing message {messageLabel} to error handler", transportMessage.GetMessageLabel());
                }
            }

            transactionContext.SetResult(commit: false, ack: false);
        }
    }

    async Task CommitIfPossible(ITransactionContext transactionContext)
    {
        if (transactionContext is not ICanEagerCommit canEagerCommit) return;
        
        await canEagerCommit.CommitAsync();
    }
}