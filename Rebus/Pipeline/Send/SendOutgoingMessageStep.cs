using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Pipeline.Send
{
    /// <summary>
    /// Outgoing step that uses the current transport to send the <see cref="TransportMessage"/>
    /// found in the context to the destination address specified by looking up
    /// <see cref="DestinationAddresses"/> in the context.
    /// </summary>
    public class SendOutgoingMessageStep : IOutgoingStep
    {
        static ILog _log;

        static SendOutgoingMessageStep()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly ITransport _transport;

        /// <summary>
        /// Constructs the step, using the specified transport to send the messages
        /// </summary>
        public SendOutgoingMessageStep(ITransport transport)
        {
            _transport = transport;
        }

        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var logicalMessage = context.Load<Message>();
            var transportMessage = context.Load<TransportMessage>();
            var currentTransactionContext = context.Load<ITransactionContext>();
            var destinationAddressesList = context.Load<DestinationAddresses>().ToList();

            var hasOneOrMoreDestinations = destinationAddressesList.Any();

            _log.Debug("Sending {0} -> {1}",
                logicalMessage.Body ?? "<empty message>",
                hasOneOrMoreDestinations ? string.Join(";", destinationAddressesList) : "<no destinations>");

            await Send(destinationAddressesList, transportMessage, currentTransactionContext);

            await next();
        }

        async Task Send(IEnumerable<string> destinationAddressesList,
            TransportMessage transportMessage,
            ITransactionContext currentTransactionContext)
        {
            var sendTasks = destinationAddressesList
                .Select(address => _transport.Send(address, transportMessage, currentTransactionContext))
                .ToArray();

            await Task.WhenAll(sendTasks);
        }
    }
}