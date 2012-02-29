using System.Collections.Generic;
using System.Linq;

namespace Rebus
{
    public class ReceivedTransportMessage
    {
        public ReceivedTransportMessage()
        {
            Headers = new Dictionary<string, string>();
        }

        /// <summary>
        /// Id given to this message, most likely by the queue infrastructure.
        /// It is important that the message can be uniquely identified - otherwise
        /// message retry will not be able to recognize the message between retries.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Message headers. Pre-defined header keys can be found in <see cref="Messages.Headers"/>.
        /// </summary>
        public IDictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Message body. Should not contain any header information.
        /// </summary>
        public byte[] Body { get; set; }

        /// <summary>
        /// String label used if the underlying message queue supports it.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Converts this message into a forwardable <see cref="TransportMessageToSend"/>.
        /// </summary>
        public TransportMessageToSend ToForwardableMessage()
        {
            var transportMessageToSend = new TransportMessageToSend
                                             {
                                                 Label = Label,
                                                 Body = Body,
                                             };
            if (Headers != null)
            {
                transportMessageToSend.Headers = Headers.ToDictionary(k => k.Key, v => v.Value);
            }
            return transportMessageToSend;
        }
    }
}