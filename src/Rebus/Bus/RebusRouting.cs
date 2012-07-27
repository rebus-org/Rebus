using System.Collections.Generic;

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
            throw new System.NotImplementedException();
        }
    }
}