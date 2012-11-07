using System.Collections.Generic;
using Rebus.Extensions;
using Rebus.Messages;

namespace Rebus.Bus
{
    /// <summary>
    /// Implements the explicitly routed messages API by using your ordinary <see cref="RebusBus"/>
    /// </summary>
    public class RebusRouting : IRebusRouting
    {
        readonly RebusBus rebusBus;

        /// <summary>
        /// Constructs the routing API with the specified <see cref="RebusBus"/>
        /// </summary>
        public RebusRouting(RebusBus rebusBus)
        {
            this.rebusBus = rebusBus;
        }

        public void Send<TCommand>(string destinationEndpoint, TCommand message)
        {
            rebusBus.PossiblyAttachSagaIdToRequest(message);

            rebusBus.InternalSend(destinationEndpoint, new List<object> { message });
        }

        public void Subscribe<TEvent>(string publisherInputQueue)
        {
            rebusBus.InternalSubscribe<TEvent>(publisherInputQueue);
        }

        public void ForwardCurrentMessage(string destinationEndpoint)
        {
            var messageContext = MessageContext.GetCurrent();
            
            var currentMessage = messageContext.CurrentMessage;
            var headers = messageContext.Headers.Clone();

            var message = new Message
                {
                    Headers = headers,
                    Messages = new[] {currentMessage}
                };

            rebusBus.InternalSend(destinationEndpoint, message);
        }
    }
}