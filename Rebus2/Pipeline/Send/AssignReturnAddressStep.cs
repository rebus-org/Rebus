using System;
using System.Threading.Tasks;
using Rebus2.Messages;
using Rebus2.Transport;

namespace Rebus2.Pipeline.Send
{
    public class AssignReturnAddressStep : IStep
    {
        readonly ITransport _transport;

        public AssignReturnAddressStep(ITransport transport)
        {
            _transport = transport;
        }

        public async Task Process(StepContext context, Func<Task> next)
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