using System;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Pipeline.Send
{
    /// <summary>
    /// Outgoing step that sets the <see cref="Headers.ReturnAddress"/> header of the outgoing message to the input queue
    /// address (found with <see cref="ITransport.Address"/>), unless the header has already been set to something else.
    /// </summary>
    [StepDocumentation("If the outgoing message does not already have a '" + Headers.ReturnAddress + "' header, this step will assign the input queue address of the sending endpoint as the return address.")]
    public class AssignReturnAddressStep : IOutgoingStep
    {
        readonly bool _hasOwnAddress;
        readonly string _address;

        /// <summary>
        /// Constructs the step, getting the input queue address from the given <see cref="ITransport"/>
        /// </summary>
        public AssignReturnAddressStep(ITransport transport)
        {
            _address = transport.Address;
            _hasOwnAddress = !string.IsNullOrWhiteSpace(_address);
        }

        /// <summary>
        /// If no return address has been added to the message, the sender's input queue address is automatically added as the <see cref="Headers.ReturnAddress"/>
        /// header
        /// </summary>
        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();
            var headers = message.Headers;

            if (_hasOwnAddress && !headers.ContainsKey(Headers.ReturnAddress))
            {
                headers[Headers.ReturnAddress] = _address;
            }

            await next();
        }
    }
}