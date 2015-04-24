using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Pipeline.Send
{
    public class SendOutgoingMessageStep : IOutgoingStep
    {
        static ILog _log;

        static SendOutgoingMessageStep()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly ITransport _transport;

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
                .Select(destinationAddress => _transport.Send(destinationAddress, transportMessage, currentTransactionContext));

            await Task.WhenAll(sendTasks);
        }
    }

    /// <summary>
    /// Encapsulates a list of destination addresses
    /// </summary>
    public class DestinationAddresses : IEnumerable<string>
    {
        readonly List<string> _addresses;

        /// <summary>
        /// Constructs the list of destination addresses
        /// </summary>
        public DestinationAddresses(IEnumerable<string> addresses)
        {
            _addresses = addresses.ToList();
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _addresses.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}