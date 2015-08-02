using System;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.Legacy
{
    [StepDocumentation("")]
    class UnpackLegacyMessageIncomingStep : IIncomingStep
    {
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();

            if (message.Headers.ContainsKey(MapLegacyHeadersIncomingStep.LegacyMessageHeader))
            {
                var body = message.Body;
                var array = body as object[];

                if (array == null)
                {
                    throw new FormatException(string.Format("Incoming message has the '{0}' header, but the message body {1} is not an object[] as expected",
                        MapLegacyHeadersIncomingStep.LegacyMessageHeader, body));
                }

                if (array.Length != 1)
                {
                    throw new FormatException(string.Format("Incoming message has the '{0}' header, and the message body is an object[] as expected, but the array has {1} elements - the legacy unpacker can only work with one single logical message in each transport message, sorry",
                        MapLegacyHeadersIncomingStep.LegacyMessageHeader, array.Length));
                }

                context.Save(new Message(message.Headers, array[0]));
            }

            await next();
        }
    }
}