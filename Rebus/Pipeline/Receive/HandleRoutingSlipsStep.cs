using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Serialization;
using Rebus.Transport;

namespace Rebus.Pipeline.Receive;

/// <summary>
/// Pipeline step that forwards routing slips when needed
/// </summary>
[StepDocumentation("If the message being handled is a routing slip with an itinerary, this step ensures that the routing slip is forwarded to the next destination.")]
public class HandleRoutingSlipsStep : IIncomingStep
{
    static readonly char[] Separators = { ';' };
    readonly ITransport _transport;
    readonly ISerializer _serialier;

    /// <summary>
    /// Creates the step
    /// </summary>
    public HandleRoutingSlipsStep(ITransport transport, ISerializer serialier)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _serialier = serialier ?? throw new ArgumentNullException(nameof(serialier));
    }

    /// <summary>
    /// Carries out the routing slip forwarding logic
    /// </summary>
    public async Task Process(IncomingStepContext context, Func<Task> next)
    {
        var message = context.Load<Message>();
        var headers = message.Headers;

        var isRoutingSlip = headers.ContainsKey(Headers.RoutingSlipItinerary);

        await next();

        if (isRoutingSlip)
        {
            await HandleRoutingSlip(context, message);
        }
    }

    async Task HandleRoutingSlip(IncomingStepContext context, Message message)
    {
        var transactionContext = context.Load<ITransactionContext>();
        var transportMessage = await _serialier.Serialize(message);
        var headers = transportMessage.Headers;

        var itinerary = GetDestinations(headers, Headers.RoutingSlipItinerary);
        var nextDestination = itinerary.FirstOrDefault();

        if (nextDestination == null)
        {
            // no more destinations - stop forwarding it now
            return;
        }

        var remainingDestinations = itinerary.Skip(1);

        transportMessage.Headers[Headers.RoutingSlipItinerary] = string.Join(";", remainingDestinations);

        IEnumerable<string> travelogue = GetDestinations(transportMessage.Headers, Headers.RoutingSlipTravelogue);
        travelogue = travelogue.Append(_transport.Address);

        transportMessage.Headers[Headers.RoutingSlipTravelogue] = string.Join(";", travelogue);
        transportMessage.Headers[Headers.CorrelationSequence] = GetNextSequenceNumber(transportMessage.Headers);

        await _transport.Send(nextDestination, transportMessage, transactionContext);
    }

    static string GetNextSequenceNumber(IReadOnlyDictionary<string, string> headers) =>
        headers.TryGetValue(Headers.CorrelationSequence, out var sequenceNumberString)
        && int.TryParse(sequenceNumberString, out var sequenceNumber)
            ? (sequenceNumber + 1).ToString()
            : "0";

    static string[] GetDestinations(Dictionary<string, string> headers, string headerKey) => 
        headers.GetValue(headerKey).Split(Separators, StringSplitOptions.RemoveEmptyEntries);
}