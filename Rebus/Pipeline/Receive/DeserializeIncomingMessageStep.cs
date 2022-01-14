using System;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Serialization;

namespace Rebus.Pipeline.Receive;

/// <summary>
/// Incoming step that gets the current <see cref="TransportMessage"/> from the context and deserializes its body,
/// saving the result as a <see cref="Message"/> back to the context.
/// </summary>
[StepDocumentation(@"Deserializes the current transport message using the configured serializer, saving the deserialized message back to the context.")]
public class DeserializeIncomingMessageStep : IIncomingStep
{
    readonly ISerializer _serializer;

    /// <summary>
    /// Constructs the step, using the specified serializer to do its thing
    /// </summary>
    public DeserializeIncomingMessageStep(ISerializer serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    /// <summary>
    /// Deserializes the incoming message by invoking the currently configured <see cref="ISerializer"/> on the <see cref="TransportMessage"/> found in the context,
    /// storing the result as the <see cref="Message"/> returned by the serializer
    /// </summary>
    public async Task Process(IncomingStepContext context, Func<Task> next)
    {
        var transportMessage = context.Load<TransportMessage>();
        var message = await _serializer.Deserialize(transportMessage);
        context.Save(message);
        await next();
    }
}