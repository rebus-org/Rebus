using System;
using System.IO;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.DataBus.ClaimCheck;

/// <summary>
/// Incoming step that 'hydrates' big messages, if the payload was stored as a data bus attachment.
/// </summary>
[StepDocumentation("Incoming step that 'hydrates' big messages, if the payload was stored as a data bus attachment.")]
public class HydrateIncomingMessageStep : IIncomingStep
{
    readonly IDataBus _dataBus;

    /// <summary>
    /// Creates the step
    /// </summary>
    public HydrateIncomingMessageStep(IDataBus dataBus)
    {
        _dataBus = dataBus ?? throw new ArgumentNullException(nameof(dataBus));
    }

    /// <summary>
    /// Hydrates the message, if it was dehydrated
    /// </summary>
    public async Task Process(IncomingStepContext context, Func<Task> next)
    {
        var transportMessage = context.Load<TransportMessage>();

        if (transportMessage.Headers.TryGetValue(Headers.MessagePayloadAttachmentId, out var attachmentId))
        {
            using var source = await _dataBus.OpenRead(attachmentId);
         
            using var destination = new MemoryStream();   
            await source.CopyToAsync(destination);

            var body = destination.ToArray();
            var headers = transportMessage.Headers.Clone();

            headers.Remove(Headers.MessagePayloadAttachmentId);

            context.Save(new TransportMessage(headers, body));
        }

        await next();
    }
}