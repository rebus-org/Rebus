using System;
using System.IO;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.DataBus.ClaimCheck;

/// <summary>
/// Outgoing step that 'dehydrates' big messages by storing the payload as a data bus attachment.
/// </summary>
[StepDocumentation("Outgoing step that 'dehydrates' big messages by storing the payload as a data bus attachment.")]
public class DehydrateOutgoingMessageStep : IOutgoingStep
{
    static readonly byte[] EmptyMessageBody = Array.Empty<byte>();

    readonly int _messageSizeLimitBytes;
    readonly IDataBus _dataBus;

    /// <summary>
    /// Creates the step
    /// </summary>
    public DehydrateOutgoingMessageStep(IDataBus dataBus, int messageSizeLimitBytes)
    {
        _dataBus = dataBus;
        _messageSizeLimitBytes = messageSizeLimitBytes;
    }

    /// <summary>
    /// Dehydrates the message, if it's too big
    /// </summary>
    public async Task Process(OutgoingStepContext context, Func<Task> next)
    {
        var transportMessage = context.Load<TransportMessage>();

        if (transportMessage.Body.Length > _messageSizeLimitBytes)
        {
            await DehydrateTransportMessage(context, transportMessage);
        }

        await next();
    }

    async Task DehydrateTransportMessage(OutgoingStepContext context, TransportMessage transportMessage)
    {
        var messageId = transportMessage.GetMessageId();

        try
        {
            using var source = new MemoryStream(transportMessage.Body);
            
            var attachment = await _dataBus.CreateAttachment(source);
            var headers = transportMessage.Headers.Clone();

            headers[Headers.MessagePayloadAttachmentId] = attachment.Id;

            context.Save(new TransportMessage(headers, EmptyMessageBody));
        }
        catch (Exception exception)
        {
            throw new RebusApplicationException(exception,
                $"Could not create (automatic claim check) attachment for outgoing message with ID {messageId}");
        }
    }
}