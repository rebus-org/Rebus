using System;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.Routing.TransportMessages
{
    /// <summary>
    /// Incoming message step that looks at the transport message and possibly forwards it to another queue
    /// </summary>
    [StepDocumentation("This step allows for very quickly forwarding of the incoming transport message without performing any further actions")]
    public class ForwardTransportMessageStep : IIncomingStep
    {
        static ILog _log;

        static ForwardTransportMessageStep()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly Func<TransportMessage, ForwardAction> _routingFunction;
        readonly ITransport _transport;

        /// <summary>
        /// Constructs the step
        /// </summary>
        /// <param name="routingFunction"></param>
        /// <param name="transport"></param>
        public ForwardTransportMessageStep(Func<TransportMessage, ForwardAction> routingFunction, ITransport transport)
        {
            _routingFunction = routingFunction;
            _transport = transport;
        }

        /// <summary>
        /// Invokes the routing function and performs some action depending on the returned <see cref="ForwardAction"/> result
        /// </summary>
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var transportMessage = context.Load<TransportMessage>();
            var routingResult = _routingFunction(transportMessage) ?? ForwardAction.None;
            var actionType = routingResult.ActionType;

            switch (actionType)
            {
                case ActionType.Forward:
                    var destinationAddresses = routingResult.DestinationAddresses;
                    var transactionContext = context.Load<ITransactionContext>();

                    _log.Debug("Forwarding {0} to {1}", transportMessage.GetMessageLabel(), string.Join(", ", destinationAddresses));

                    await Task.WhenAll(
                        destinationAddresses
                            .Select(address => _transport.Send(address, transportMessage, transactionContext))
                        );
                    break;

                case ActionType.None:
                    await next();
                    break;

                default:
                    await next();
                    break;
            }
        }
    }
}