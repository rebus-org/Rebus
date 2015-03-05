using System;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Serialization;

namespace Rebus.Pipeline.Send
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