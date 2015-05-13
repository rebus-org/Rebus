using System;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Serialization;

namespace Rebus.Pipeline.Send
{
    /// <summary>
    /// Outgoing step that gets the current <see cref="Message"/> from the context and serializes its body,
    /// saving the result as a <see cref="TransportMessage"/> back to the context.
    /// </summary>
    public class SerializeOutgoingMessageStep : IOutgoingStep
    {
        readonly ISerializer _serializer;

        /// <summary>
        /// Constructs the step, using the specified serializer to do its thing
        /// </summary>
        public SerializeOutgoingMessageStep(ISerializer serializer)
        {
            _serializer = serializer;
        }

        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var logicalMessage = context.Load<Message>();
            var transportMessage = await _serializer.Serialize(logicalMessage);
            
            context.Save(transportMessage);

            await next();
        }
    }
}