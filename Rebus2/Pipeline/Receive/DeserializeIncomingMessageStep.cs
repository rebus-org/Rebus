using System;
using System.Threading.Tasks;
using Rebus2.Messages;
using Rebus2.Serialization;

namespace Rebus2.Pipeline.Receive
{
    public class DeserializeIncomingMessageStep : IIncomingStep
    {
        readonly ISerializer _serializer;

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