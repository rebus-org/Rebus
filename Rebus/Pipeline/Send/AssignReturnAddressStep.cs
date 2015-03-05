using System;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Pipeline.Send
{
    public class AssignReturnAddressStep : IOutgoingStep
    {
        readonly ITransport _transport;

        public AssignReturnAddressStep(ITransport transport)
        {
            _transport = transport;
        }

        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();
            var headers = message.Headers;

            if (!string.IsNullOrWhiteSpace(_transport.Address) && !headers.ContainsKey(Headers.ReturnAddress))
            {
                headers[Headers.ReturnAddress] = _transport.Address;
            }

            await next();
        }
    }
}