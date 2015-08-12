using System;
using System.Collections.Generic;

namespace Rebus.IdempotentSagas
{
    /// <summary>
    /// Results (side effects) of handling a message by an idempotent saga.
    /// </summary>
    public class IdempotentSagaResults
    {
        #region SideEffects inner class
        /// <summary>
        /// A transport message sent during the handling of the processed message.
        /// </summary>
        public class SideEffect
        {
            /// <summary>
            /// Gets the date.
            /// </summary>
            public DateTime Date { get; set; }
            /// <summary>
            /// The destinations of the message.
            /// </summary>
            public IEnumerable<string> Destinations { get; set; }
            /// <summary>
            /// The headers of the message.
            /// </summary>
            public IDictionary<string, object> Headers { get; set; }
            /// <summary>
            /// The logical message's body inside the transport message.
            /// </summary>
            public byte[] Message { get; set; }
            /// <summary>
            /// Gets the type of the serializer.
            /// </summary>
            /// <remarks>
            /// This may be usefull in case of migrations so we can perform in-place upgrades, etc.
            /// </remarks>
            public string Serializer { get; set; }

            public SideEffect()
            {
            }

            public SideEffect(IEnumerable<string> destinations, IDictionary<string, object> headers, byte[] message, Type serializerType)
            {
                Date = DateTime.UtcNow;
                Destinations = destinations;
                Headers = headers;
                Message = message;
                Serializer = serializerType.AssemblyQualifiedName;
            }
        }
        #endregion

        /// <summary>
        /// The id of the processed message.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Gets the message's headers.
        /// </summary>
        public IDictionary<string, object> Headers { get; set; }
        /// <summary>
        /// The message.
        /// </summary>
        public byte[] Message { get; set; }
        /// <summary>
        /// The serializer type used to serialize the <see cref="Message"/>.
        /// </summary>
        /// <remarks>
        /// This may be usefull in case of migrations so we can perform in-place upgrades, etc.
        /// </remarks>
        public string Serializer { get; set; }
        /// <summary>
        /// The messages sent during the processed message handling.
        /// </summary>
        public IList<SideEffect> SideEffects { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IdempotentSagaResults"/> class.
        /// </summary>
        public IdempotentSagaResults()
        {
            SideEffects = new List<SideEffect>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IdempotentSagaResults" /> class.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="message">The message.</param>
        /// <param name="serializerType">Type of the serializer.</param>
        public IdempotentSagaResults(string id, TransportMessageToSend message, Type serializerType)
            : this()
        {
            Id = id;
            Headers = message.Headers;
            Message = message.Body;
            Serializer = serializerType.AssemblyQualifiedName;
        }
    }
}
