using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            /// The destinations of the message.
            /// </summary>
            public IEnumerable<string> Destinations { get; private set; }
            /// <summary>
            /// The headers of the message.
            /// </summary>
            public IDictionary<string, object> Headers { get; private set; }
            /// <summary>
            /// The logical message's body inside the transport message.
            /// </summary>
            public byte[] Message { get; private set; }
            /// <summary>
            /// Gets the type of the serializer.
            /// </summary>
            /// <value>
            /// The type of the serializer.
            /// </value>
            public string Serializer { get; private set; }

            public SideEffect()
            {
            }

            public SideEffect(IEnumerable<string> destinations, IDictionary<string, object> headers, byte[] message, Type serializerType)
            {
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
        public string Id { get; private set; }
        /// <summary>
        /// The message.
        /// </summary>
        public object Message { get; private set; }
        /// <summary>
        /// The messages sent during the processed message handling.
        /// </summary>
        public IList<SideEffect> SideEffects { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IdempotentSagaResults"/> class.
        /// </summary>
        public IdempotentSagaResults()
        {
            SideEffects = new List<SideEffect>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IdempotentSagaResults"/> class.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="message">The message.</param>
        public IdempotentSagaResults(string id, object message)
            : base()
        {
            Id = id;
            Message = message;
        }
    }
}
