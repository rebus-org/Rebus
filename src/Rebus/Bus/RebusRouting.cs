using System;
using System.Collections.Generic;
using Rebus.Extensions;
using Rebus.Messages;

namespace Rebus.Bus
{
    public class RebusRouting : IRebusRouting
    {
        readonly RebusBus rebusBus;

        public RebusRouting(RebusBus rebusBus)
        {
            this.rebusBus = rebusBus;
        }

        public void Send<TCommand>(string destinationEndpoint, TCommand message)
        {
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