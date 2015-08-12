using System;
using System.Collections.Generic;
using Rebus.Extensions;

namespace Rebus
{
    /// <summary>
    /// Message container that contains the received parts of a single transport message.
    /// It carries a headers dictionary, a body byte array, and an ID which is
    /// most like assigned by the infrastructure. The ID is used to track retries in the event that
    /// delivery fails.
    /// </summary>
    [Serializable]
    public class ReceivedTransportMessage
    {
        /// <summary>
        /// Constructs the wrapper of a transport message that has been received
        /// </summary>
        public ReceivedTransportMessage()
        {
            Headers = new Dictionary<string, object>();
        }

        /// <summary>
        /// Id given to this message, most likely by the queue infrastructure.
        /// It is important that the message can be uniquely identified - otherwise
        /// message retry will not be able to recognize the message between retries.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Message headers. Pre-defined header keys can be found in <see cref="Shared.Headers"/>.
        /// </summary>
        public IDictionary<string, object> Headers { get; set; }

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
            var transportMessageToSend =
                new TransportMessageToSend
                {
                    Label = Label,
                    Body = Body,
                };

            if (Headers != null)
            {
                transportMessageToSend.Headers = Headers.Clone();
            }
            
            return transportMessageToSend;
        }

        /// <summary>
        /// Gets the header with the specified key as a string if possible, otherwise null
        /// </summary>
        public string GetStringHeader(string key)
        {
            return Headers.ContainsKey(key) && Headers[key] is string ? (string) Headers[key] : null;
        }
    }
}