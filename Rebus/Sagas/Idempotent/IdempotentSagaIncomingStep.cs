using System;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Transport;

namespace Rebus.Sagas.Idempotent
{
    /// <summary>
    /// Incoming pipeline step that checks the loaded saga data to see if the message currently being handled is a dupe.
    /// If it is, message dispatch is skipped, but any messages stored as outgoing messages from previously handling the incoming message
    /// will be sent 
    /// </summary>
    [StepDocumentation(@"Checks the loaded saga data to see if the message currently being handled is a duplicate of a message that has previously been handled.

If that is the case, message dispatch is skipped, but any messages stored as outgoing messages from previously handling the incoming message will be sent.")]
    public class IdempotentSagaIncomingStep : IIncomingStep
    {
        readonly ITransport _transport;
        readonly ILog _log;

        /// <summary>
        /// Constructs the step
        /// </summary>
        public IdempotentSagaIncomingStep(ITransport transport, IRebusLoggerFactory rebusLoggerFactory)
        {
            _transport = transport;
            _log = rebusLoggerFactory.GetCurrentClassLogger();
        }

        /// <summary>
        /// Checks the loaded saga data to see if the message currently being handled is a duplicate of a message that has previously been handled. 
        /// If that is the case, message dispatch is skipped, but any messages stored as outgoing messages from previously handling the incoming message will be sent.
        /// </summary>
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var handlerInvokersForSagas = context.Load<HandlerInvokers>()
                .Where(l => l.HasSaga)
                .ToList();

            var message = context.Load<Message>();
            var messageId = message.GetMessageId();

            var transactionContext = context.Load<ITransactionContext>();

            foreach (var handlerInvoker in handlerInvokersForSagas)
            {
                var sagaData = handlerInvoker.GetSagaData() as IIdempotentSagaData;

                if (sagaData == null) continue;

                var idempotencyData = sagaData.IdempotencyData
                                      ?? (sagaData.IdempotencyData = new IdempotencyData());

                if (idempotencyData.HasAlreadyHandled(messageId))
                {
                    _log.Info("Message with ID {0} has already been handled by saga with ID {1}",
                        messageId, sagaData.Id);

                    var outgoingMessages = idempotencyData
                        .GetOutgoingMessages(messageId)
                        .ToList();

                    if (outgoingMessages.Any())
                    {
                        _log.Info("Found {0} outgoing messages to be (re-)sent... will do that now",
                            outgoingMessages.Count);

                        foreach (var messageToResend in outgoingMessages)
                        {
                            foreach (var destinationAddress in messageToResend.DestinationAddresses)
                            {
                                await
                                    _transport.Send(destinationAddress, messageToResend.TransportMessage,
                                        transactionContext);
                            }
                        }
                    }
                    else
                    {
                        _log.Info("Found no outgoing messages to be (re-)sent...");
                    }

                    handlerInvoker.SkipInvocation();
                }
                else
                {
                    idempotencyData.MarkMessageAsHandled(messageId);
                }
            }

            await next();
        }
    }
}