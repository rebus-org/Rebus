using System;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Serialization;

namespace Rebus.Pipeline.Receive
{
    /// <summary>
    /// Incoming step that gets the current <see cref="TransportMessage"/> from the context and deserializes its body,
    /// saving the result as a <see cref="Message"/> back to the context.
    /// </summary>
    public class DeserializeIncomingMessageStep : IIncomingStep
    {
        readonly ISerializer _serializer;

        /// <summary>
        /// Constructs the step, using the specified serializer to do its thing
        /// </summary>
        public DeserializeIncomingMessageStep(ISerializer serializer)
        {
            _serializer = serializer;
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var transportMessage = context.Load<TransportMessage>();
            var message = await _serializer.Deserialize(transportMessage);
            
            context.Save(message);
            
            await next();
        }
    }
}