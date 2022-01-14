using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Pipeline.Send;

/// <summary>
/// Outgoing step that uses the current transport to send the <see cref="TransportMessage"/>
/// found in the context to the destination address specified by looking up
/// <see cref="DestinationAddresses"/> in the context.
/// </summary>
[StepDocumentation("Final step that uses the current transport to send the transport message found in the context to all addresses found by looking up the DestinationAddress object from the context.")]
public class SendOutgoingMessageStep : IOutgoingStep
{
    readonly ITransport _transport;
    readonly ILog _log;

    /// <summary>
    /// Constructs the step, using the specified transport to send the messages
    /// </summary>
    public SendOutgoingMessageStep(ITransport transport, IRebusLoggerFactory rebusLoggerFactory)
    {
        if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _log = rebusLoggerFactory.GetLogger<SendOutgoingMessageStep>();
    }

    /// <summary>
    /// Sends the outgoing message using the configured <see cref="ITransport"/>, sending to the <see cref="DestinationAddresses"/>
    /// found in the <see cref="OutgoingStepContext"/>.
    /// </summary>
    public async Task Process(OutgoingStepContext context, Func<Task> next)
    {
        var logicalMessage = context.Load<Message>();
        var transportMessage = context.Load<TransportMessage>();
        var currentTransactionContext = context.Load<ITransactionContext>();
        var destinationAddressesList = context.Load<DestinationAddresses>().ToList();

        var hasOneOrMoreDestinations = destinationAddressesList.Any();

        _log.Debug("Sending {messageBody} -> {queueNames}",
            logicalMessage.Body ?? "<empty message>",
            hasOneOrMoreDestinations ? string.Join(";", destinationAddressesList) : "<no destinations>");

        await Send(destinationAddressesList, transportMessage, currentTransactionContext);

        await next();
    }

    async Task Send(List<string> destinationAddressesList, TransportMessage transportMessage, ITransactionContext currentTransactionContext)
    {
        var sendTasks = destinationAddressesList
            .Select(async address => await _transport.Send(address, transportMessage, currentTransactionContext))
            .ToArray();

        await Task.WhenAll(sendTasks);
    }
}