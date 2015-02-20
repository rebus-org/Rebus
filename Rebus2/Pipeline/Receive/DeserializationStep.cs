using System;
using System.Threading.Tasks;
using Rebus2.Messages;
using Rebus2.Serialization;

namespace Rebus2.Pipeline.Receive
{
    public class DeserializationStep : IStep
    {
        readonly ISerializer _serializer;

        public DeserializationStep(ISerializer serializer)
        {
            _serializer = serializer;
        }

        public async Task Process(StepContext context, Func<Task> next)
        {
            var transportMessage = context.Load<TransportMessage>();
            var message = await _serializer.Deserialize(transportMessage);
            
            context.Save(message);
            
            await next();
        }
    }
}