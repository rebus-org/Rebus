using System.Collections.Generic;

namespace Rebus
{
    public class ReceivedTransportMessage
    {
        /// <summary>
        /// Id given to this message, most likely by the queue infrastructure.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Data of whatever header and body information this message may contain.
        /// </summary>
        public string Data { get; set; }

        public IDictionary<string, string> Headers { get; set; }

        public string Label { get; set; }

        public TransportMessageToSend ToForwardableMessage()
        {
            return new TransportMessageToSend
                       {
                           Label = Label,
                           Data = Data,
                       };
        }
    }
}