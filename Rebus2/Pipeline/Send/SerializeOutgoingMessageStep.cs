using System;
using System.Threading.Tasks;
using Rebus2.Messages;
using Rebus2.Serialization;

namespace Rebus2.Pipeline.Send
{
    public class SerializeOutgoingMessageStep : IOutgoingStep
    {
        readonly ISerializer _serializer;

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