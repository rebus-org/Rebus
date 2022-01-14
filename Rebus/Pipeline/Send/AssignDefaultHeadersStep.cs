using Rebus.Messages;
using System;
using System.Threading.Tasks;
using Rebus.Serialization;
using Rebus.Transport;
using Rebus.Time;

namespace Rebus.Pipeline.Send;

/// <summary>
/// Outgoing step that sets default headers of the outgoing message.
/// If the <see cref="Headers.MessageId"/> header has not been set, it is set to a new GUID.
/// If the bus is not a one-way client, the <see cref="Headers.ReturnAddress"/> header is set to the address of the transport (unless the header has already been set to something else)
/// If the <see cref="Headers.SentTime"/> header has not been set, it is set to <see cref="DateTimeOffset.Now"/>.
/// If the <see cref="Headers.Type"/> header has not been set, it is set to the simple assembly-qualified name of the send message type
/// </summary>
[StepDocumentation(@"Assigns these default headers to the outgoing message: 

1) a new GUID as the 'rbs2-msg-id' header (*).

2) a 'rbs2-return-address' (unless the bus is a one-way client) (*).

3) a 'rbs2-senttime' with the current time (*).

-4) 'rbs2-msg-type' with the message's simple assembly-qualified type name (*).
-
-(*) Unless explicitly set to something else")]
public class AssignDefaultHeadersStep : IOutgoingStep
{
    readonly IMessageTypeNameConvention _messageTypeNameConvention;
    readonly IRebusTime _rebusTime;
    readonly string _senderAddress;
    readonly string _returnAddress;
    readonly bool _hasOwnAddress;

    /// <summary>
    /// Constructs the step, getting the input queue address from the given <see cref="ITransport"/>
    /// </summary>
    public AssignDefaultHeadersStep(ITransport transport, IRebusTime rebusTime, IMessageTypeNameConvention messageTypeNameConvention, string defaultReturnAddressOrNull)
    {
        _rebusTime = rebusTime ?? throw new ArgumentNullException(nameof(rebusTime));
        _messageTypeNameConvention = messageTypeNameConvention ?? throw new ArgumentNullException(nameof(messageTypeNameConvention));
        _senderAddress = transport.Address;
        _returnAddress = defaultReturnAddressOrNull ?? transport.Address;
        _hasOwnAddress = !string.IsNullOrWhiteSpace(_senderAddress);
    }

    /// <summary>
    /// Executes the step and sets the default headers
    /// </summary>
    public async Task Process(OutgoingStepContext context, Func<Task> next)
    {
        var message = context.Load<Message>();
        var headers = message.Headers;

        if (!headers.ContainsKey(Headers.MessageId))
        {
            headers[Headers.MessageId] = Guid.NewGuid().ToString();
        }

        if (_hasOwnAddress && !headers.ContainsKey(Headers.ReturnAddress))
        {
            headers[Headers.ReturnAddress] = _returnAddress;
        }

        if (!headers.ContainsKey(Headers.SentTime))
        {
            headers[Headers.SentTime] = _rebusTime.Now.ToString("O");
        }

        if (_hasOwnAddress)
        {
            headers[Headers.SenderAddress] = _senderAddress;
        }
        else
        {
            headers.Remove(Headers.SenderAddress);
        }

        if (!headers.ContainsKey(Headers.Type))
        {
            var messageType = message.Body.GetType();

            headers[Headers.Type] = _messageTypeNameConvention.GetTypeName(messageType);
        }

        await next();
    }
}